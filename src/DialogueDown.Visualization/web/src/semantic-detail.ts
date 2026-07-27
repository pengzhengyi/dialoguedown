import type { DisplayNode } from "./model";
import { nodeDetailTitle, nodeDetailBody, NODE_DETAIL_PLACEHOLDER } from "./detail-panel";
import { initCollapsiblePanel } from "./collapse-toggle";

/** The Semantic tab's node-details panel, plus its element to mount atop the tables column. */
export interface NodeDetailPanel {
    element: HTMLElement;
    /** Show a selected node's details, expanding the panel if it was collapsed. */
    show(node: DisplayNode): void;
    /** Reset to the "nothing selected" placeholder. */
    clear(): void;
}

/**
 * The Semantic tab's node-details panel: a collapsible panel pinned to the top of the tables
 * column (sticky, so it never scrolls out of view while the tables scroll beneath it). Clicking
 * a scene or a script block in the tree shows its attributes, source, and a rendered preview
 * here; selecting a node auto-expands the panel so the detail is always revealed. It reuses the
 * report's collapsible-panel mechanics and the shared node-detail rendering.
 */
export function createNodeDetailPanel(): NodeDetailPanel {
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

    return {
        element: panel,
        show(node) {
            body.innerHTML =
                `<div class="node-detail-heading">${nodeDetailTitle(node)}</div>` +
                nodeDetailBody(node);
            if (collapsible.isCollapsed()) {
                collapsible.toggle(); // reveal the detail the reader just asked for
                reflect();
            }
        },
        clear() {
            body.innerHTML = NODE_DETAIL_PLACEHOLDER;
        },
    };
}
