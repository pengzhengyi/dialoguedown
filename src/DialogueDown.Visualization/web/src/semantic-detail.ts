import type { DisplayNode, Span } from "./model";
import { nodeDetailTitle, nodeDetailBody, NODE_DETAIL_PLACEHOLDER } from "./detail-panel";
import { createJumpButton, type JumpButton } from "./jump-button";
import { initCollapsiblePanel } from "./collapse-toggle";
import { mountPreviewHtml } from "./preview-html";
import { mermaidPreviews } from "./mermaid-preview";

/** How the Semantic tab's node-details panel participates in navigation. */
export interface NodeDetailPanelOptions {
    /** Jump to the shown node's source in the Source tab (selecting a span, or placing the caret
     *  for a synthetic node). Absent when there is no Source tab to jump to. */
    jumpToSource?: (span: Span) => void;
    /** The active stage has recognized Dialogue jump syntax. */
    recognizeJumps?: boolean;
}

/** The Semantic tab's node-details panel, plus its element to mount atop the tables column. */
export interface NodeDetailPanel {
    element: HTMLElement;
    /** Show a selected node's details, expanding the panel if it was collapsed. */
    show(node: DisplayNode): void;
    /** Reset to the "nothing selected" placeholder. */
    clear(): void;
    /** Release preview work retained for this panel. */
    destroy(): void;
}

/**
 * The Semantic tab's node-details panel: a collapsible panel pinned to the top of the tables
 * column (sticky, so it never scrolls out of view while the tables scroll beneath it). Clicking
 * a scene or a script block in the tree shows its attributes, source, and a rendered preview
 * here; selecting a node auto-expands the panel so the detail is always revealed. It reuses the
 * report's collapsible-panel mechanics, the shared node-detail rendering, and — beside the title
 * — the shared {@link createJumpButton} affordance to the Source tab.
 */
export function createNodeDetailPanel(options: NodeDetailPanelOptions = {}): NodeDetailPanel {
    const panel = document.createElement("section");
    panel.className = "table-panel node-detail-panel";

    const toggle = document.createElement("button");
    toggle.type = "button";
    toggle.className = "table-panel-toggle";
    toggle.innerHTML = `<span class="table-panel-caret" aria-hidden="true"></span>`;
    const title = document.createElement("span");
    title.className = "table-panel-title";
    title.textContent = "Node details";
    toggle.appendChild(title);

    // Same header shape as the tables' panels (a caret/title toggle in a header bar), so the
    // "Node details" title reads like the other table titles rather than a filled button.
    const header = document.createElement("div");
    header.className = "table-panel-header";
    header.appendChild(toggle);

    const body = document.createElement("div");
    body.className = "table-panel-body node-detail-body";
    body.innerHTML = NODE_DETAIL_PLACEHOLDER;

    panel.append(header, body);

    const collapsible = initCollapsiblePanel({
        container: panel,
        collapsedClass: "collapsed",
        storageKey: "dd-sem-node-detail",
        name: "node details",
    });
    const reflect = (): void =>
        toggle.setAttribute("aria-expanded", String(!collapsible.isCollapsed()));
    toggle.addEventListener("click", () => {
        collapsible.toggle();
        reflect();
    });
    reflect();

    const jump: JumpButton | null = options.jumpToSource
        ? createJumpButton(options.jumpToSource)
        : null;

    return {
        element: panel,
        show(node) {
            mountPreviewHtml(
                body,
                `<div class="node-detail-heading">${nodeDetailTitle(node)}</div>` +
                    nodeDetailBody(node, {
                        recognizeJumps: options.recognizeJumps ?? false,
                    }),
            );
            void mermaidPreviews.renderNow(body);
            if (jump) {
                body.querySelector(".node-detail-heading")?.appendChild(jump.element);
                jump.update(node);
            }
            if (collapsible.isCollapsed()) {
                collapsible.toggle(); // reveal the detail the reader just asked for
                reflect();
            }
        },
        clear() {
            mermaidPreviews.dispose(body);
            body.innerHTML = NODE_DETAIL_PLACEHOLDER;
            jump?.update(null);
        },
        destroy() {
            mermaidPreviews.dispose(body);
        },
    };
}
