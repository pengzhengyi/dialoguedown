import {
    RangeSet,
    StateEffect,
    StateField,
    type EditorState,
    type Extension,
    type Transaction,
} from "@codemirror/state";
import {
    Decoration,
    EditorView,
    GutterMarker,
    ViewPlugin,
    gutter,
    type DecorationSet,
    type ViewUpdate,
} from "@codemirror/view";
import type { BreakpointBinding, DebugController, DebugSnapshot } from "./debug-controller";

interface BreakpointState {
    positions: readonly number[];
    verifiedLines: ReadonlySet<number>;
}

interface DebugVisualState {
    snapshot: DebugSnapshot | null;
    decorations: DecorationSet;
}

const toggleBreakpointEffect = StateEffect.define<number>();
const setBreakpointBindingsEffect = StateEffect.define<readonly BreakpointBinding[]>();
const setDebugSnapshotEffect = StateEffect.define<DebugSnapshot>();

const breakpointField = StateField.define<BreakpointState>({
    create: () => ({ positions: [], verifiedLines: new Set() }),
    update(value, transaction) {
        let positions = value.positions;
        let verifiedLines = value.verifiedLines;

        if (transaction.docChanged) {
            positions = mapBreakpointPositions(positions, transaction);
            verifiedLines = new Set();
        }

        for (const effect of transaction.effects) {
            if (effect.is(toggleBreakpointEffect)) {
                const position = transaction.state.doc.lineAt(
                    clamp(effect.value, 0, transaction.state.doc.length),
                ).from;
                positions = positions.includes(position)
                    ? positions.filter((candidate) => candidate !== position)
                    : uniqueSorted([...positions, position]);
            } else if (effect.is(setBreakpointBindingsEffect)) {
                verifiedLines = new Set(
                    effect.value
                        .filter((binding) => binding.verified)
                        .map((binding) => binding.line),
                );
            }
        }

        if (positions === value.positions && verifiedLines === value.verifiedLines) return value;
        return { positions, verifiedLines };
    },
});

const debugVisualField = StateField.define<DebugVisualState>({
    create: () => ({ snapshot: null, decorations: Decoration.none }),
    update(value, transaction) {
        let snapshot = value.snapshot;
        let decorations = transaction.docChanged
            ? value.decorations.map(transaction.changes)
            : value.decorations;

        for (const effect of transaction.effects) {
            if (effect.is(setDebugSnapshotEffect)) {
                snapshot = effect.value;
                decorations = currentLineDecorations(transaction.state, snapshot);
            }
        }
        return { snapshot, decorations };
    },
    provide: (field) => EditorView.decorations.from(field, (value) => value.decorations),
});

class BreakpointMarker extends GutterMarker {
    public constructor(private readonly verified: boolean) {
        super();
    }

    public eq(other: BreakpointMarker): boolean {
        return other.verified === this.verified;
    }

    public toDOM(): Node {
        const marker = document.createElement("span");
        marker.className = this.verified
            ? "dd-debug-breakpoint dd-debug-breakpoint-verified"
            : "dd-debug-breakpoint dd-debug-breakpoint-unverified";
        marker.title = this.verified ? "Verified breakpoint" : "Unverified breakpoint";
        return marker;
    }
}

const verifiedBreakpointMarker = new BreakpointMarker(true);
const unverifiedBreakpointMarker = new BreakpointMarker(false);
const breakpointSpacer = new (class extends GutterMarker {
    public toDOM(): Node {
        const spacer = document.createElement("span");
        spacer.className = "dd-debug-breakpoint-spacer";
        return spacer;
    }
})();

const currentArrowMarker = new (class extends GutterMarker {
    public toDOM(): Node {
        const marker = document.createElement("span");
        marker.className = "dd-debug-current-arrow";
        marker.textContent = "▶";
        marker.setAttribute("aria-label", "Current execution line");
        return marker;
    }
})();

const executionSpacer = new (class extends GutterMarker {
    public toDOM(): Node {
        const spacer = document.createElement("span");
        spacer.className = "dd-debug-execution-spacer";
        spacer.textContent = "▶";
        return spacer;
    }
})();

const breakpointGutter = gutter({
    class: "dd-debug-breakpoint-gutter",
    markers: (view) => breakpointMarkers(view.state),
    initialSpacer: () => breakpointSpacer,
    lineMarkerChange: breakpointFieldChanged,
    domEventHandlers: {
        mousedown(view, line) {
            toggleBreakpointAt(view, line.from);
            return true;
        },
    },
});

const executionGutter = gutter({
    class: "dd-debug-execution-gutter",
    markers: (view) => executionMarkers(view.state),
    initialSpacer: () => executionSpacer,
    lineMarkerChange: debugVisualFieldChanged,
});

/** Add breakpoint/current-line state, gutters, and controller synchronization to an editor. */
export function debugEditor(controller: DebugController): Extension {
    const bridge = ViewPlugin.define((view) => new DebugEditorBridge(view, controller));
    return [breakpointField, breakpointGutter, debugVisualField, executionGutter, bridge];
}

/** Toggle the requested breakpoint on the line containing `position`. Exported for testing. */
export function toggleBreakpointAt(view: EditorView, position: number): void {
    view.dispatch({ effects: toggleBreakpointEffect.of(position) });
}

/** The requested breakpoint lines in the editor's current document (one-based). */
export function requestedBreakpointLines(state: EditorState): number[] {
    return state
        .field(breakpointField)
        .positions.map((position) => state.doc.lineAt(position).number);
}

class DebugEditorBridge {
    private destroyed = false;
    private queued = false;
    private pendingSnapshot: DebugSnapshot | null = null;
    private lastSnapshot: DebugSnapshot | null = null;
    private readonly unsubscribe: () => void;

    public constructor(
        private readonly view: EditorView,
        private readonly controller: DebugController,
    ) {
        this.unsubscribe = controller.subscribe((snapshot) => this.queueSnapshot(snapshot));
        this.queueSnapshot(controller.snapshot());
    }

    public update(update: ViewUpdate): void {
        const before = update.startState.field(breakpointField).positions;
        const after = update.state.field(breakpointField).positions;
        if (update.docChanged) this.controller.sourceChanged();
        if (before !== after) {
            this.controller.setBreakpoints(requestedBreakpointLines(update.state));
        }
    }

    public destroy(): void {
        this.destroyed = true;
        this.unsubscribe();
    }

    private queueSnapshot(snapshot: DebugSnapshot): void {
        this.pendingSnapshot = snapshot;
        if (this.queued) return;
        this.queued = true;
        queueMicrotask(() => {
            this.queued = false;
            if (this.destroyed || this.pendingSnapshot === null) return;
            const next = this.pendingSnapshot;
            this.pendingSnapshot = null;
            const reveal = shouldRevealDebugLocation(this.lastSnapshot, next);
            this.lastSnapshot = next;
            const effects: StateEffect<unknown>[] = [
                setDebugSnapshotEffect.of(next),
                setBreakpointBindingsEffect.of(next.breakpoints),
            ];
            if (reveal && next.location) {
                const line = lineAtNumber(this.view.state, next.location.line);
                effects.push(EditorView.scrollIntoView(line.from, { y: "center" }));
            }
            this.view.dispatch({
                effects,
            });
        });
    }
}

/** Whether a snapshot moves execution to a location the editor has not already revealed. */
export function shouldRevealDebugLocation(
    previous: DebugSnapshot | null,
    next: DebugSnapshot,
): boolean {
    if (!next.location || (next.status !== "paused" && next.status !== "awaiting-path")) {
        return false;
    }
    const previousWasPaused = previous?.status === "paused" || previous?.status === "awaiting-path";
    return !previousWasPaused || previous?.location?.id !== next.location.id;
}

function breakpointMarkers(state: EditorState): RangeSet<GutterMarker> {
    const value = state.field(breakpointField);
    return RangeSet.of(
        value.positions.map((position) => {
            const line = state.doc.lineAt(position);
            const marker = value.verifiedLines.has(line.number)
                ? verifiedBreakpointMarker
                : unverifiedBreakpointMarker;
            return marker.range(line.from);
        }),
        true,
    );
}

function executionMarkers(state: EditorState): RangeSet<GutterMarker> {
    const snapshot = state.field(debugVisualField).snapshot;
    if (
        !snapshot?.location ||
        (snapshot.status !== "paused" && snapshot.status !== "awaiting-path")
    ) {
        return RangeSet.empty;
    }
    return RangeSet.of([
        currentArrowMarker.range(lineAtNumber(state, snapshot.location.line).from),
    ]);
}

function currentLineDecorations(state: EditorState, snapshot: DebugSnapshot): DecorationSet {
    if (
        !snapshot.location ||
        (snapshot.status !== "paused" && snapshot.status !== "awaiting-path")
    ) {
        return Decoration.none;
    }
    const line = lineAtNumber(state, snapshot.location.line);
    return Decoration.set([Decoration.line({ class: "dd-debug-current-line" }).range(line.from)]);
}

function breakpointFieldChanged(update: ViewUpdate): boolean {
    return update.startState.field(breakpointField) !== update.state.field(breakpointField);
}

function debugVisualFieldChanged(update: ViewUpdate): boolean {
    return update.startState.field(debugVisualField) !== update.state.field(debugVisualField);
}

function lineAtNumber(state: EditorState, lineNumber: number) {
    return state.doc.line(clamp(lineNumber, 1, state.doc.lines));
}

function uniqueSorted(values: readonly number[]): number[] {
    return [...new Set(values)].sort((a, b) => a - b);
}

function mapBreakpointPositions(positions: readonly number[], transaction: Transaction): number[] {
    if (isWholeDocumentReplacement(transaction)) {
        return uniqueSorted(
            positions.map((position) => {
                const oldLine = transaction.startState.doc.lineAt(position).number;
                return transaction.state.doc.line(clamp(oldLine, 1, transaction.state.doc.lines))
                    .from;
            }),
        );
    }

    return uniqueSorted(
        positions.flatMap((position) => {
            const lineNumber = transaction.startState.doc.lineAt(position).number;
            if (lineWasDeleted(transaction, lineNumber)) return [];
            const mapped = transaction.changes.mapPos(position, 1);
            return [transaction.state.doc.lineAt(mapped).from];
        }),
    );
}

function isWholeDocumentReplacement(transaction: Transaction): boolean {
    let ranges = 0;
    let wholeReplacement = false;
    transaction.changes.iterChangedRanges((fromA, toA, fromB, toB) => {
        ranges += 1;
        wholeReplacement = fromA === 0 && toA === transaction.startState.doc.length && toB > fromB;
    });
    return ranges === 1 && wholeReplacement;
}

function lineWasDeleted(transaction: Transaction, lineNumber: number): boolean {
    const document = transaction.startState.doc;
    const line = document.line(lineNumber);
    const deletionRanges: Array<{ from: number; to: number }> = [];
    if (lineNumber < document.lines) {
        deletionRanges.push({ from: line.from, to: line.to + 1 });
    }
    if (lineNumber > 1) {
        deletionRanges.push({ from: line.from - 1, to: line.to });
    }
    let deleted = false;
    transaction.changes.iterChangedRanges((fromA, toA, fromB, toB) => {
        if (
            fromB === toB &&
            deletionRanges.some((range) => fromA <= range.from && toA >= range.to)
        ) {
            deleted = true;
        }
    });
    return deleted;
}

function clamp(value: number, min: number, max: number): number {
    return Math.max(min, Math.min(max, value));
}
