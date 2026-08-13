import type { Stage } from "./model";
import type { DisplayNode, Span } from "./model";
import { createTreeView, type TreeView, type TreeViewOptions } from "./tree-view";
import { createTablePanel } from "./semantic-table";
import { createNodeDetailPanel } from "./semantic-detail";
import { createEntityHighlighter } from "./entity-highlight";
import { initCollapsiblePanel } from "./collapse-toggle";

/** A built Semantic tab: its element to mount, and the scene-tree view for camera memory. */
export interface SemanticView {
    element: HTMLElement;
    view: TreeView;
    /** Release resources owned by the semantic-only side panels. */
    destroy(): void;
}

const MIN_TABLES_WIDTH = 220;
const MAX_TABLES_WIDTH = 560;

/**
 * Build the Semantic tab's analytics layout: the scene tree as an interactive graph on the
 * left (reusing the tree view, so zoom, fold, full screen, and position memory work as on the
 * other tabs) and the speaker, anchor, and jump-resolution tables stacked as collapsible
 * panels on the right. A draggable divider re-sizes the tables column and hosts a toggle that
 * hides the whole column to give the graph full width. Cross-link highlighting is wired across
 * the whole tab, so hovering a scene, speaker, or jump anywhere lights it up everywhere.
 */
export function createSemanticView(
    stage: Stage,
    onSelect: (node: DisplayNode) => void,
    options: TreeViewOptions,
    jumpToSource?: (span: Span) => void,
    recognizeJumps = false,
): SemanticView {
    const container = document.createElement("div");
    container.className = "semantic-view";

    const graph = document.createElement("div");
    graph.className = "semantic-graph";

    const divider = document.createElement("div");
    divider.className = "semantic-divider";
    divider.title = "Drag to resize";

    const tables = document.createElement("div");
    tables.className = "semantic-tables";

    // Clicking a scene or block in the tree shows its detail in a panel pinned to the top of
    // the tables column; selecting a node also drives the shared inspector (hidden on this tab).
    const nodeDetail = createNodeDetailPanel({
        ...(jumpToSource ? { jumpToSource } : {}),
        recognizeJumps,
    });
    const view = createTreeView(
        stage,
        (node) => {
            onSelect(node);
            nodeDetail.show(node);
        },
        options,
    );
    graph.append(view.svg, view.legend, view.controls);
    tables.appendChild(nodeDetail.element);
    for (const table of stage.tables ?? []) {
        tables.appendChild(createTablePanel(table));
    }

    container.append(graph, divider, tables);
    initTablesResizer(container, tables, divider);

    // The whole tables column can be hidden to give the graph full width. Its toggle lives on
    // the divider, doubling as the always-present re-open handle; the choice persists.
    const tablesPanel = initCollapsiblePanel({
        container,
        collapsedClass: "tables-collapsed",
        storageKey: "dd-semantic-tables-collapsed",
        name: "tables",
    });
    divider.appendChild(tablesPanel.button);

    createEntityHighlighter(container);
    return {
        element: container,
        view,
        destroy: () => nodeDetail.destroy(),
    };
}

/**
 * Wire the divider so dragging it resizes the tables column (via `--semantic-tables-width`).
 * Pointer capture keeps the drag self-contained — no document-level listeners that would
 * accumulate each time the tab is rebuilt on a Live Edit save.
 */
function initTablesResizer(
    container: HTMLElement,
    tables: HTMLElement,
    divider: HTMLElement,
): void {
    let rightEdge = 0;

    divider.addEventListener("pointerdown", (event) => {
        // A collapsed column has nothing to resize — the divider is just its re-open handle.
        if (container.classList.contains("tables-collapsed")) return;
        // Pressing the toggle should collapse, not start a drag.
        if ((event.target as HTMLElement).closest(".collapse-toggle")) return;
        // When the layout stacks (narrow screens), the divider is a horizontal collapse bar
        // only — a width drag doesn't apply, so ignore it there.
        if (getComputedStyle(container).flexDirection === "column") return;
        rightEdge = tables.getBoundingClientRect().right || window.innerWidth;
        divider.setPointerCapture(event.pointerId);
        document.body.style.userSelect = "none";
        event.preventDefault();
    });

    divider.addEventListener("pointermove", (event) => {
        if (!divider.hasPointerCapture(event.pointerId)) return;
        const width = Math.min(
            MAX_TABLES_WIDTH,
            Math.max(MIN_TABLES_WIDTH, rightEdge - event.clientX),
        );
        container.style.setProperty("--semantic-tables-width", `${width}px`);
    });

    const release = (event: PointerEvent): void => {
        if (divider.hasPointerCapture(event.pointerId))
            divider.releasePointerCapture(event.pointerId);
        document.body.style.userSelect = "";
    };
    divider.addEventListener("pointerup", release);
    divider.addEventListener("pointercancel", release);
}
