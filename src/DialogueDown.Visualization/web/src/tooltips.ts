import { delegate, followCursor } from "tippy.js";

/**
 * Rich, accessible hover tooltips (Tippy.js) over the graph's nodes and routes, showing a node's
 * full label and attributes, or a route's kind, meaning, and the words the writer gave it (from
 * their `data-tip`). Delegation covers nodes and routes added later on expand.
 *
 * The two are placed differently because their shapes differ. A node is a small box, so its
 * tooltip sits beside the box. A route is a long, curved line whose bounding box spans much of the
 * drawing — anchoring to it would put the tooltip nowhere near the line the reader is pointing at —
 * so a route's tooltip opens at the pointer instead, where the reader is already looking.
 */
export function initTooltips(parent: Element): void {
    delegate(parent, {
        target: "g.node",
        allowHTML: true,
        maxWidth: 340,
        delay: [120, 0],
        content: (reference) => reference.getAttribute("data-tip") ?? "",
    });
    delegate(parent, {
        target: "path.edge-hit",
        allowHTML: true,
        maxWidth: 340,
        delay: [120, 0],
        // Opened where the pointer entered, rather than tracking it: a route is followed by eye,
        // and a tooltip sliding along it would be the moving thing in a still picture.
        followCursor: "initial",
        plugins: [followCursor],
        content: (reference) => reference.getAttribute("data-tip") ?? "",
    });
}

/**
 * Hover tooltips (Tippy.js) over the stage tabs, showing each stage's description
 * (from its `data-tip`). Delegation covers tabs rebuilt on a live re-render.
 */
export function initTabTooltips(tabsBar: Element): void {
    delegate(tabsBar, {
        target: "button.tab",
        placement: "bottom",
        delay: [200, 0],
        content: (reference) => reference.getAttribute("data-tip") ?? "",
    });
}
