import { EditorSelection, type EditorState } from "@codemirror/state";
import { EditorView, showDialog, type Command, type KeyBinding } from "@codemirror/view";

/**
 * Go to line, shaped like VS Code's: a small box that floats over the text with the field on one
 * line and a sentence under it saying what pressing Enter will do. No button — Enter goes, Escape
 * and clicking away dismiss.
 *
 * CodeMirror ships this command, and the tab used it first. Its dialog cannot take this shape:
 * `showDialog` decides the button and the placement from the config the command passes, and the
 * command passes neither `content` nor `top`. Rendering the dialog here is what buys the second
 * line, and the second line is the point — the expression syntax below is worth far more than a
 * plain line number, and nothing else in the report would ever teach it.
 *
 * The cost is that the expression is parsed here rather than upstream. It is spent once: the
 * sentence a reader reads and the position the cursor lands on are computed by {@link resolve}
 * together, so the dialog can never promise a line it does not then go to.
 */

/** Where a Go to line expression lands, once resolved against the document. */
interface GotoTarget {
    /** A line number, already clamped into the document. */
    readonly line: number;
    /** A column within that line, clamped when the jump is made. */
    readonly column: number;
    /** Whether the expression named a line outside the document and was pulled back inside. */
    readonly clamped: boolean;
}

/**
 * A line, optionally signed for a relative jump, optionally suffixed `%` for a position in the
 * document, and optionally followed by `:column`. Mirrors the expression CodeMirror's own
 * `gotoLine` accepts, so a reader who knows one knows the other.
 */
const EXPRESSION = /^\s*([+-])?(\d+)?(:\d+)?(%)?\s*$/;

/** Resolves a typed expression, or null when it names nothing to go to. */
export function resolve(state: EditorState, value: string): GotoTarget | null {
    const match = EXPRESSION.exec(value);
    if (!match) return null;
    const [, sign, digits, colon, percent] = match;
    if (digits == null && colon == null) return null;

    const start = state.doc.lineAt(state.selection.main.head);
    let line = digits == null ? start.number : Number(digits);
    if (digits != null && percent != null) {
        let fraction = line / 100;
        if (sign != null) {
            fraction = fraction * (sign === "-" ? -1 : 1) + start.number / state.doc.lines;
        }
        line = Math.round(state.doc.lines * fraction);
    } else if (digits != null && sign != null) {
        line = line * (sign === "-" ? -1 : 1) + start.number;
    }

    const clamped = Math.max(1, Math.min(state.doc.lines, line));
    return {
        line: clamped,
        column: colon == null ? 0 : Number(colon.slice(1)),
        clamped: clamped !== line,
    };
}

/** The sentence under the field: what Enter will do, or what to type when it would do nothing. */
export function guidanceFor(state: EditorState, value: string): string {
    const target = resolve(state, value);
    if (target == null) {
        return `Type a line between 1 and ${state.doc.lines} — also 12:5, +10, -10, or 50%.`;
    }
    const where =
        target.column > 0
            ? `line ${target.line} at column ${target.column}`
            : `line ${target.line}`;
    return target.clamped
        ? `Press Enter to go to ${where}, the nearest line in the document.`
        : `Press Enter to go to ${where}.`;
}

/** Moves the cursor to a resolved target and brings it into view. */
function goTo(view: EditorView, target: GotoTarget): void {
    const line = view.state.doc.line(target.line);
    const selection = EditorSelection.cursor(
        line.from + Math.max(0, Math.min(target.column, line.length)),
    );
    view.dispatch({
        selection,
        effects: EditorView.scrollIntoView(selection.from, { y: "center" }),
        scrollIntoView: true,
    });
}

/** The dialog's body: the field, then the sentence that tracks it. */
function dialogBody(view: EditorView, close: () => void): HTMLElement {
    const form = document.createElement("form");
    form.className = "dd-goto-form";

    const field = document.createElement("input");
    field.type = "text";
    field.name = "line";
    // Deliberately not `cm-textfield`: CodeMirror's base style for that class is light-themed and
    // is injected after this stylesheet, so it would paint a white field on the dark theme.
    field.className = "dd-goto-input";
    field.setAttribute("aria-label", "Go to line");
    field.value = String(view.state.doc.lineAt(view.state.selection.main.head).number);

    const guidance = document.createElement("div");
    guidance.className = "dd-goto-guidance";
    // Announced politely so the sentence reaches a screen reader as the field is typed into.
    guidance.setAttribute("role", "status");
    guidance.textContent = guidanceFor(view.state, field.value);

    field.addEventListener("input", () => {
        guidance.textContent = guidanceFor(view.state, field.value);
    });

    // Clicking away dismisses it, the way a quick input does. The check is deferred because focus
    // is briefly nowhere while it moves, and skipped when it lands back inside the dialog.
    form.addEventListener("focusout", () => {
        setTimeout(() => {
            if (!form.contains(form.ownerDocument.activeElement)) close();
        }, 0);
    });

    form.append(field, guidance);
    return form;
}

/**
 * Opens the dialog. Enter and Escape are wired by `showDialog` because the content holds a form;
 * the jump itself runs when that form resolves.
 */
export const gotoLine: Command = (view) => {
    const { result } = showDialog(view, {
        content: (dialogView, close) => dialogBody(dialogView, close),
        class: "dd-goto",
        top: true,
        focus: "input",
    });
    result.then((form) => {
        if (form == null) return;
        const field = form.elements.namedItem("line");
        const target = field instanceof HTMLInputElement ? resolve(view.state, field.value) : null;
        if (target != null) goTo(view, target);
    }, console.error);
    return true;
};

/**
 * VS Code's Go to Line binding, beside the `Mod-Alt-g` CodeMirror already provides.
 *
 * `Ctrl-g` is literal Control on every platform, which is exactly what VS Code binds — on macOS
 * `Cmd-g` stays Find Next there as it does here, and on Windows and Linux `F3` keeps Find Next
 * when this takes `Ctrl-g` over. It is listed before `searchKeymap` so it wins that overlap.
 *
 * `Mod-l` is not an option however tempting it reads: the browser owns `Cmd/Ctrl-L` for the
 * address bar and a page cannot take it back. A synthetic key event in a test would still
 * "press" it, so a test would pass while no reader could ever use it.
 */
export const gotoLineKeymap: readonly KeyBinding[] = [
    { key: "Ctrl-g", run: gotoLine, preventDefault: true },
    { key: "Mod-Alt-g", run: gotoLine, preventDefault: true },
];
