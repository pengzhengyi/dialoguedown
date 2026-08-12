import type { DisplayEdge, DisplayNode, Stage } from "./model";
import { CATEGORY_COLORS } from "./palette";
import { edgeStyle } from "./edge-style";
import { tintsOf } from "./region-bands";

import { baseLabel } from "./text";

export interface CategoryStat {
    names: string[];
    count: number;
}

/** Per-category node counts and the distinct type names present (for the legend). */
export function categoryStats(nodes: DisplayNode[]): Record<string, CategoryStat> {
    const stats: Record<string, CategoryStat> = {};
    for (const node of nodes) {
        if (!node.category) continue;
        const stat = (stats[node.category] ??= { names: [], count: 0 });
        stat.count += 1;
        // A node's explicit type name wins (a scene's label is its title, not its kind);
        // otherwise the label is the type (an AST node labels itself by type).
        const name = node.typeName ?? baseLabel(node.label);
        if (!stat.names.includes(name)) stat.names.push(name);
    }
    return stats;
}

/** How many nodes each region holds, in the order the stage first names them. */
export function regionCounts(nodes: DisplayNode[]): Map<string, number> {
    const counts = new Map<string, number>();
    for (const node of nodes) {
        if (node.region) counts.set(node.region, (counts.get(node.region) ?? 0) + 1);
    }
    return counts;
}

/** How many edges of each category the stage draws, for the edge legend. */
export function edgeCategoryCounts(edges: DisplayEdge[]): Record<string, number> {
    const counts: Record<string, number> = {};
    for (const edge of edges) {
        if (!edge.category) continue;
        counts[edge.category] = (counts[edge.category] ?? 0) + 1;
    }
    return counts;
}

export interface LegendHandlers {
    onToggle(category: string, dimmed: boolean): void;
    onHover(category: string): void;
    onLeave(): void;
}

/**
 * Build the interactive legend for a stage: one row per category present, showing
 * its color, the stage's own type name(s), and a node count. Clicking a row
 * toggles it (dimming); hovering highlights it.
 */
export function createLegend(stage: Stage, handlers: LegendHandlers): HTMLElement {
    const stats = categoryStats(stage.nodes);
    const dimmed = new Set<string>();

    const legend = document.createElement("div");
    legend.className = "legend";

    // The legend floats over the drawing it describes, and it has grown — nodes, edges, and now
    // regions. A reader who has learned it can fold it away and have the canvas back.
    const fold = document.createElement("button");
    fold.type = "button";
    fold.className = "legend-fold";
    fold.setAttribute("aria-expanded", "true");
    fold.setAttribute("aria-label", "Hide the legend");
    fold.title = "Hide the legend";
    fold.addEventListener("click", () => {
        const open = fold.getAttribute("aria-expanded") === "true";
        fold.setAttribute("aria-expanded", String(!open));
        fold.setAttribute("aria-label", open ? "Show the legend" : "Hide the legend");
        fold.title = open ? "Show the legend" : "Hide the legend";
        legend.classList.toggle("folded", open);
    });
    legend.append(fold);

    const nodeItems: HTMLElement[] = [];
    for (const category of Object.keys(CATEGORY_COLORS)) {
        const stat = stats[category];
        if (stat) {
            nodeItems.push(legendItem(category, stat.names.join(" / "), stat.count));
        }
    }

    // Edges only carry a meaning on a stage that has kinds of route to tell apart, so the edge
    // group appears there and nowhere else — and with it, the heading that names the other group.
    const edgeCounts = edgeCategoryCounts(stage.edges);
    const edgeItems = Object.keys(CATEGORY_COLORS)
        .filter((category) => edgeCounts[category])
        .map((category) => edgeItem(category, edgeCounts[category]));

    // A region is a third kind of thing the drawing shows, so it earns its own group — and its
    // rows behave as the others do: hover to pick it out, click to fade it.
    const regions = regionCounts(stage.nodes);
    const tints = tintsOf(stage.nodes.map((node) => node.region));
    // A region is grouped under the kind of grouping it is — a scene today, a file later — so the
    // legend already has a shelf to put the next kind on.
    const byKind = new Map<string, HTMLElement[]>();
    for (const [name, count] of regions) {
        const kind = stage.regions?.find((each) => each.name === name)?.kind ?? "Region";
        const rows = byKind.get(kind) ?? [];
        rows.push(regionItem(name, tints.get(name) ?? 0, count));
        byKind.set(kind, rows);
    }

    if (edgeItems.length > 0 || byKind.size > 0) {
        legend.append(groupHeading("Nodes"), ...nodeItems);
        if (edgeItems.length > 0) legend.append(groupHeading("Edges"), ...edgeItems);
        if (byKind.size > 0) {
            legend.append(groupHeading("Regions"));
            for (const [kind, rows] of byKind) legend.append(kindGroup(kind, rows));
        }
    } else {
        legend.append(...nodeItems);
    }
    return legend;

    /** One kind of region, as a disclosure the reader can fold away when it is not the question. */
    function kindGroup(kind: string, rows: HTMLElement[]): HTMLElement {
        const group = document.createElement("div");
        group.className = "legend-kind";

        const toggle = document.createElement("button");
        toggle.type = "button";
        toggle.className = "legend-kind-toggle";
        toggle.setAttribute("aria-expanded", "true");
        toggle.textContent = `${kind} ${rows.length === 1 ? "region" : "regions"}`;

        const body = document.createElement("div");
        body.className = "legend-kind-body";
        body.append(...rows);

        toggle.addEventListener("click", () => {
            const open = toggle.getAttribute("aria-expanded") === "true";
            toggle.setAttribute("aria-expanded", String(!open));
            body.hidden = open;
        });

        group.append(toggle, body);
        return group;
    }

    function regionItem(name: string, tint: number, count: number): HTMLButtonElement {
        // The region's own name is its key: the legend's dim and highlight channel is keyed by
        // string, and a region name cannot collide with a palette category.
        const item = interactiveItem(name, name, count);
        item.classList.add("legend-region");
        item.dataset.region = name;
        const swatch = item.querySelector<HTMLElement>(".swatch")!;
        swatch.className = "swatch region-swatch";
        swatch.dataset.tint = String(tint);
        swatch.style.background = "";
        return item;
    }

    function groupHeading(text: string): HTMLElement {
        const heading = document.createElement("p");
        heading.className = "legend-heading";
        heading.textContent = text;
        return heading;
    }

    // An edge row names the route and shows a line rather than a dot, so it never reads as a node.
    // It answers to the pointer exactly as a node row does: a route is as worth isolating as a
    // kind of node, and a row that looks alike but behaves differently only misleads.
    function edgeItem(category: string, count: number): HTMLButtonElement {
        const style = edgeStyle(category);
        const item = interactiveItem(category, style?.label ?? category, count);
        item.classList.add("legend-edge");

        // The swatch draws the line's own pattern, so the legend shows what to look for rather
        // than only what color it is.
        const swatch = item.querySelector<HTMLElement>(".swatch")!;
        swatch.classList.add("edge-swatch");
        if (style?.symbol) {
            // A line the graph stamps with a glyph shows the glyph here too, rather than a dash
            // pattern the reader will not find anywhere on the drawing.
            swatch.classList.add("symbol-swatch");
            swatch.textContent = SYMBOL_GLYPHS[style.symbol];
            // The shared builder painted a bar here; a glyph is drawn in ink, not filled.
            swatch.style.background = "";
            swatch.style.color = CATEGORY_COLORS[category];
            return item;
        }
        swatch.style.background = style?.dash
            ? `repeating-linear-gradient(to right, ${CATEGORY_COLORS[category]} 0 ${dashOn(style.dash)}px, transparent ${dashOn(style.dash)}px ${dashPeriod(style.dash)}px)`
            : CATEGORY_COLORS[category];
        return item;
    }

    function legendItem(category: string, typeName: string, count: number): HTMLButtonElement {
        return interactiveItem(category, typeName, count);
    }

    function interactiveItem(category: string, name: string, count: number): HTMLButtonElement {
        const item = document.createElement("button");
        item.type = "button";
        item.className = "legend-item";
        item.setAttribute("aria-pressed", "true");

        const swatch = document.createElement("span");
        swatch.className = "swatch";
        swatch.style.background = CATEGORY_COLORS[category];

        const label = document.createElement("span");
        label.className = "legend-label";
        label.textContent = name;

        const countEl = document.createElement("span");
        countEl.className = "count";
        countEl.textContent = String(count);

        item.append(swatch, label, countEl);

        item.addEventListener("click", () => {
            const nowDimmed = !dimmed.has(category);
            if (nowDimmed) dimmed.add(category);
            else dimmed.delete(category);
            item.classList.toggle("muted", nowDimmed);
            item.setAttribute("aria-pressed", String(!nowDimmed));
            handlers.onToggle(category, nowDimmed);
            // Drop the hover preview the pointer is holding, or the row the reader just switched
            // off would keep showing at full strength and the click would look like it did
            // nothing. Leaving and returning brings the preview back.
            handlers.onLeave();
        });
        item.addEventListener("mouseenter", () => handlers.onHover(category));
        item.addEventListener("focus", () => handlers.onHover(category));
        item.addEventListener("mouseleave", handlers.onLeave);
        item.addEventListener("blur", handlers.onLeave);
        return item;
    }
}

/** How a stamped glyph is written in the legend, where there is no path to stamp it along. */
const SYMBOL_GLYPHS: Record<string, string> = { cross: "\u00d7\u00d7\u00d7" };

// A dash pattern is "on off"; the swatch repeats that at the same proportions.
function dashOn(dash: string): number {
    return Number(dash.split(/\s+/)[0]);
}

function dashPeriod(dash: string): number {
    const [on, off] = dash.split(/\s+/).map(Number);
    return on + off;
}
