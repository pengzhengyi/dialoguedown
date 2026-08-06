import { StateEffect, StateField, type Extension } from "@codemirror/state";
import { EditorView, showPanel, type Panel, type ViewUpdate } from "@codemirror/view";
import tippy, { type Instance } from "tippy.js";
import { codicon } from "./codicon";
import type { ReservedTarget, ReservedTargetRole } from "./model";
import { copyToClipboard } from "./path-display";
import { showToast } from "./toast";

const ROLE_MARKER: Record<ReservedTargetRole, string> = {
    Entry: "▶",
    Terminal: "∞",
};

/** The paste-ready Markdown jump link for one compiler-projected reserved target. */
export function reservedTargetJumpLink(target: ReservedTarget): string {
    return `[${target.label}](#${target.anchor})`;
}

/** Replace the compiler-projected reserved targets shown in the fixed editor panel. */
const setReservedTargetsEffect = StateEffect.define<readonly ReservedTarget[]>();

/**
 * The reserved-target list lives in editor state so a save/hot reload can update the panel
 * without rebuilding CodeMirror. The panel is absent for an empty list.
 */
const reservedTargetsField = StateField.define<readonly ReservedTarget[]>({
    create: () => [],
    update(targets, transaction) {
        for (const effect of transaction.effects) {
            if (effect.is(setReservedTargetsEffect)) return [...effect.value];
        }
        return targets;
    },
    provide: (field) =>
        showPanel.from(field, (targets) =>
            targets.length > 0 ? createReservedTargetsPanel : null,
        ),
});

/** Install the always-available panel seam; it stays invisible until targets are pushed. */
export function reservedTargetsPanel(): Extension {
    return reservedTargetsField;
}

/** Push the latest compiler metadata into an existing Source editor. */
export function setEditorReservedTargets(
    view: EditorView,
    targets: readonly ReservedTarget[],
): void {
    view.dispatch({ effects: setReservedTargetsEffect.of(targets) });
}

function createReservedTargetsPanel(view: EditorView): Panel {
    return new ReservedTargetsPanel(view);
}

class ReservedTargetsPanel implements Panel {
    public readonly dom: HTMLElement;
    public readonly top = false;

    private tips: Instance[] = [];

    public constructor(private readonly view: EditorView) {
        this.dom = document.createElement("div");
        this.dom.className = "dd-reserved-targets";
        this.dom.setAttribute("role", "region");
        this.dom.setAttribute("aria-label", "Reserved jump targets");
        this.render();
    }

    public mount(): void {
        this.syncGutterMetrics();
    }

    public update(update: ViewUpdate): void {
        if (
            update.startState.field(reservedTargetsField) !==
            update.state.field(reservedTargetsField)
        ) {
            this.render();
        }
        if (update.geometryChanged) this.syncGutterMetrics();
    }

    public destroy(): void {
        this.destroyTips();
    }

    private render(): void {
        this.destroyTips();
        const rows = this.view.state.field(reservedTargetsField).map((target) => this.row(target));
        this.dom.replaceChildren(...rows);
        this.syncGutterMetrics();
    }

    private row(target: ReservedTarget): HTMLElement {
        const row = document.createElement("div");
        row.className = "dd-reserved-target-row";
        row.dataset.role = target.role;

        const marker = document.createElement("span");
        marker.className = "dd-reserved-target-marker";
        marker.textContent = ROLE_MARKER[target.role];
        marker.setAttribute("aria-hidden", "true");

        const copy = reservedTargetJumpLink(target);
        const button = document.createElement("button");
        button.type = "button";
        button.className = "dd-reserved-target-copy";
        button.setAttribute("aria-label", `Copy jump link to ${target.label}`);
        button.append(
            textSpan("dd-reserved-target-label", target.label),
            textSpan("dd-reserved-target-anchor", `#${target.anchor}`),
            codicon("link", "dd-reserved-target-link-icon"),
        );
        button.addEventListener("click", () => {
            void copyToClipboard(copy).then(() => showToast(`Copied ${copy}`));
        });
        this.tips.push(tippy(button, { content: copy, placement: "top" }));

        row.append(marker, button);
        return row;
    }

    /**
     * Mirror CodeMirror's real gutter so the faux row lines up with the source above it: the column
     * spans the whole gutter width, and the sentinel marker right-aligns under the line-number
     * digits (matching their right edge and padding) instead of floating centered across the fold
     * gutter too.
     */
    private syncGutterMetrics(): void {
        this.view.requestMeasure({
            key: this,
            read: (view) => {
                const gutters = view.dom.querySelector<HTMLElement>(".cm-gutters");
                if (!gutters) return null;
                const guttersRight = gutters.getBoundingClientRect().right;
                const digits = view.dom.querySelector<HTMLElement>(
                    ".cm-lineNumbers .cm-gutterElement:last-child",
                );
                const digitsRight = digits
                    ? digits.getBoundingClientRect().right -
                      (parseFloat(getComputedStyle(digits).paddingRight) || 0)
                    : guttersRight;
                return {
                    width: gutters.getBoundingClientRect().width,
                    markerPadRight: Math.max(0, guttersRight - digitsRight),
                };
            },
            write: (metrics) => {
                if (!metrics || metrics.width <= 0) return;
                this.dom.style.setProperty("--dd-editor-gutter-width", `${metrics.width}px`);
                this.dom.style.setProperty(
                    "--dd-reserved-marker-pad-right",
                    `${metrics.markerPadRight}px`,
                );
            },
        });
    }

    private destroyTips(): void {
        for (const tip of this.tips) tip.destroy();
        this.tips = [];
    }
}

function textSpan(className: string, text: string): HTMLSpanElement {
    const span = document.createElement("span");
    span.className = className;
    span.textContent = text;
    return span;
}
