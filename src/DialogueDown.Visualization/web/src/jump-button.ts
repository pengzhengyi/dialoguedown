import tippy from "tippy.js";
import { codicon } from "./codicon";
import type { DisplayNode } from "./model";
import type { Span } from "./span-splice";

/**
 * A reusable **Jump to source** icon button for a node-details panel. It sits beside the node
 * title as an icon-only button — a Tippy tooltip carries the "Jump to source" label — and, for
 * the shown node, takes the reader to its span in the Source tab: selecting a real node's text,
 * or placing the caret at a synthetic node's zero-width position. Both node-detail panels (the
 * graph inspector and the Semantic tab's) mount one, so the affordance is identical on each.
 */
export interface JumpButton {
    /** The button element to place in a node title row. */
    readonly element: HTMLButtonElement;
    /** Reflect the shown node: hidden when it maps to no position, else armed with its span. */
    update(node: DisplayNode | null): void;
}

const JUMP_LABEL = "Jump to source";

export function createJumpButton(jumpToSource: (span: Span) => void): JumpButton {
    let current: DisplayNode | null = null;

    const button = document.createElement("button");
    button.type = "button";
    button.className = "node-jump";
    button.setAttribute("aria-label", JUMP_LABEL);
    button.append(codicon("go-to-file", "node-jump-icon"));
    tippy(button, { content: JUMP_LABEL, maxWidth: 280 });

    button.addEventListener("click", () => {
        const span = current?.span;
        if (span) jumpToSource(span);
    });

    return {
        element: button,
        update(node) {
            current = node;
            button.hidden = node?.span == null;
        },
    };
}
