import type { DisplayNode, Span } from "./model";
import { colorOf } from "./palette";
import { ellipsize, escapeHtml, renderNodePreview } from "./text";
import { mountPreviewHtml } from "./preview-html";
import { mermaidPreviews } from "./mermaid-preview";

/** How much of a content node's words its detail row shows before the full text below. */
const MAX_TITLE_TEXT = 80;
/** The longest label that still reads as a heading rather than as content. */
const MAX_TITLE_LABEL = 40;
/**
 * How much of a node's words a table cell keeps.
 *
 * The visible clipping is the stylesheet's — one line, ellipsised to whatever width the column
 * has — because a character count cannot know how wide a panel the reader has dragged. This is
 * only a bound on how much text is written into the document at all.
 */
const MAX_CELL_TEXT = 120;

// A cell's words, in an element the stylesheet can clip to one line.
function cellText(label: string): string {
    return `<span class="cell-text">${escapeHtml(ellipsize(label, MAX_CELL_TEXT))}</span>`;
}
import { createJumpButton, type JumpButton } from "./jump-button";
import { codicon } from "./codicon";
import { edgeStyle } from "./edge-style";
import type { Neighbor, Neighbors } from "./neighbors";
import type { BorderCrossing, CrossingEnd, RegionDetail } from "./region-detail";

/** One end of an edge, as the inspector shows it. */
export interface EdgeEnd {
    id: string;
    label: string;
    category?: string;
}

/** An edge the reader has asked about: what kind of route it is, and the two nodes it joins. */
export interface EdgeDetail {
    category?: string;
    source: EdgeEnd;
    target: EdgeEnd;
}

export interface DetailPanelOptions {
    /**
     * Jump to the selected node's source in the Source tab — selecting its span, or placing the
     * cursor at a synthetic node's zero-width position. Absent when there is no Source tab to jump
     * to (a single-graph render), which also hides the inspector's jump affordance.
     */
    jumpToSource?: (span: Span) => void;
    /**
     * Select another node by id, so a neighbor row can take the reader to the node it names.
     * Absent when nothing is listening, which also makes the rows plain text.
     */
    selectNode?: (id: string) => void;
    /** Show the route between two nodes, so an edge cell opens the edge it names. */
    selectEdge?: (fromId: string, toId: string) => void;
    /** Show a region, so the region a node sits in is a way into that region. */
    selectRegion?: (region: string) => void;
    /** Light up a node, a route, or a region while the pointer rests on the cell naming it. */
    highlight?: (
        what: { nodeId?: string; fromId?: string; toId?: string; region?: string } | null,
    ) => void;
}

export interface DetailPanel {
    show(node: DisplayNode, preview?: NodePreviewOptions): void;
    /**
     * Say how the reader arrived, when the answer is not the thing they asked about.
     *
     * A reverse jump resolves a source selection to the node that encloses it, which for text the
     * compiler left out is a node the text never became. Silently showing that node reads as "this
     * is what it turned into" -- the opposite of the truth -- so the panel says otherwise. Cleared
     * by the next selection, which arrived on its own terms.
     */
    note(message: string | null): void;
    /** Show a route rather than a node: what it means, and the two nodes it joins. */
    showEdge(edge: EdgeDetail): void;
    /** Show a region: how much it holds, its border, and the text it was written as. */
    showRegion(region: RegionDetail, source?: string, preview?: NodePreviewOptions): void;
    clear(): void;
}

export interface NodePreviewOptions {
    /** The active stage has recognized Dialogue jump syntax. */
    recognizeJumps?: boolean;
    /**
     * What leads to this node and what it leads to. Given only by a stage whose edges carry a
     * meaning — in a tree, a node's parent and children are already plain from the drawing.
     */
    neighbors?: Neighbors;
    /** The tint the node's region is drawn with, so its name wears the band's color here too. */
    regionTint?: number;
}

/** The body HTML shown when no node is selected. */
export const NODE_DETAIL_PLACEHOLDER =
    "<p>Click any node to see the source it was produced from, and a rendered preview. " +
    "Use <strong>Jump to source</strong> to edit it in the Source tab.</p>";

/**
 * The title HTML for a node's detail: a category color dot beside what the node *is*.
 *
 * A node whose label carries content — a line of dialogue — is titled by its kind instead, and
 * the words themselves become the first detail below. A whole paragraph in a heading crowds out
 * the panel, and the same text is already spelled out under Source and Preview.
 */
export function nodeDetailTitle(node: DisplayNode): string {
    return (
        categoryDot(node.category) + escapeHtml(titlesByKind(node) ? node.typeName! : node.label)
    );
}

/**
 * Whether the node's label is too long to serve as a title.
 *
 * A scene's title names it in three words and belongs in the heading; a line of dialogue can run a
 * paragraph and crowds the panel out. Length is what separates them — not the stage they came
 * from — so short labels keep their heading wherever they are.
 */
function titlesByKind(node: DisplayNode): boolean {
    return Boolean(node.typeName) && node.label.length > MAX_TITLE_LABEL;
}

/** The body HTML for a node's detail: its attributes, then its source and a rendered preview. */
export function nodeDetailBody(node: DisplayNode, preview: NodePreviewOptions = {}): string {
    return (
        regionSection(node, preview.regionTint) +
        attributesTable(contentRow(node).concat(node.attributes)) +
        neighborSections(preview.neighbors) +
        sourceSection(node, preview)
    );
}

/**
 * The graph tabs' node inspector: a selected node's category, attributes, the source it was
 * produced from, and a rendered preview.
 *
 * It is deliberately **read-only**. Editing lives in one place — the Source tab — and the
 * **Jump to source** action beside the title takes the reader there with the node's span already
 * selected. This panel and the Semantic tab's node-details panel therefore render identically
 * through {@link nodeDetailTitle} and {@link nodeDetailBody}; only where they mount differs.
 */
export function createDetailPanel(options: DetailPanelOptions = {}): DetailPanel {
    const titleEl = document.getElementById("detail-title")!;
    const bodyEl = document.getElementById("detail-body")!;

    // A "Jump to source" affordance (served or export, whenever there is a Source tab): an icon
    // button placed beside the node title that takes the reader to the node's span in the Source
    // editor. Shown only for a node that maps to a position — which includes a synthetic node
    // (a zero-width caret). Reused, not rebuilt, across selections.
    const jump: JumpButton | null = options.jumpToSource
        ? createJumpButton(options.jumpToSource)
        : null;

    // Render the title, then re-append the reused jump button (setting the title HTML drops any
    // prior child) and reflect the node on it.
    function renderTitle(html: string, node: DisplayNode | null): void {
        titleEl.innerHTML = html;
        if (jump) {
            titleEl.appendChild(jump.element);
            jump.update(node);
        }
    }

    bodyEl.innerHTML = NODE_DETAIL_PLACEHOLDER;

    function note(message: string | null): void {
        bodyEl.querySelector(".dd-detail-note")?.remove();
        if (message == null) return;
        const callout = document.createElement("p");
        callout.className = "dd-detail-note";
        callout.append(codicon("info", "dd-detail-note-icon"), document.createTextNode(message));
        bodyEl.prepend(callout);
    }

    // One delegated listener rather than one per row: the body is rewritten on every selection,
    // and a listener bound to a row would go with it.
    bodyEl.addEventListener("click", (event) => {
        const target = event.target as Element | null;
        const node = target?.closest<HTMLElement>("button.neighbor");
        if (node?.dataset.nodeId) {
            options.selectNode?.(node.dataset.nodeId);
            return;
        }
        const route = target?.closest<HTMLElement>("button.route");
        if (route?.dataset.fromId && route.dataset.toId) {
            options.selectEdge?.(route.dataset.fromId, route.dataset.toId);
            return;
        }
        const region = target?.closest<HTMLElement>("button.region-link");
        if (region?.dataset.region) options.selectRegion?.(region.dataset.region);
    });

    // Hovering a cell lights the thing it names in the drawing, so a row and a line are plainly
    // the same object seen twice.
    bodyEl.addEventListener("mouseover", (event) => onHover(event.target as Element | null));
    bodyEl.addEventListener("mouseout", (event) => {
        const leaving = (event.target as Element | null)?.closest(
            "button.neighbor, button.route, button.region-link",
        );
        if (leaving) options.highlight?.(null);
    });

    function onHover(target: Element | null): void {
        const node = target?.closest<HTMLElement>("button.neighbor");
        if (node?.dataset.nodeId) {
            options.highlight?.({ nodeId: node.dataset.nodeId });
            return;
        }
        const route = target?.closest<HTMLElement>("button.route");
        if (route?.dataset.fromId && route.dataset.toId) {
            options.highlight?.({ fromId: route.dataset.fromId, toId: route.dataset.toId });
            return;
        }
        const region = target?.closest<HTMLElement>("button.region-link");
        if (region?.dataset.region) options.highlight?.({ region: region.dataset.region });
    }

    return {
        note,
        show(node, preview = {}) {
            renderTitle(nodeDetailTitle(node), node);
            mountPreviewHtml(bodyEl, nodeDetailBody(node, preview));
            void mermaidPreviews.renderNow(bodyEl);
        },
        showRegion(region, source, preview = {}) {
            // A region is declared by its heading, so it offers the same jump a node does — and
            // lands on the words that name it rather than on the lines beneath them.
            renderTitle(
                regionDetailTitle(region),
                region.declaredAt
                    ? {
                          id: region.name,
                          label: region.name,
                          attributes: [],
                          span: region.declaredAt,
                      }
                    : null,
            );
            mountPreviewHtml(bodyEl, regionDetailBody(region, source, preview));
            void mermaidPreviews.renderNow(bodyEl);
        },
        showEdge(edge) {
            // An edge has no source span of its own, so it offers no jump — the nodes it joins do.
            mermaidPreviews.dispose(bodyEl);
            renderTitle(edgeDetailTitle(edge.category), null);
            bodyEl.innerHTML = edgeDetailBody(edge);
        },
        clear() {
            mermaidPreviews.dispose(bodyEl);
            renderTitle(escapeHtml("Node details"), null);
            bodyEl.innerHTML = NODE_DETAIL_PLACEHOLDER;
        },
    };
}

// A color dot ties the node to its legend color without repeating a category
// name (the node's own label already appears beside it).
function categoryDot(category: string | undefined): string {
    if (!category) return "";
    return `<span class="dot" style="background:${colorOf(category)}"></span>`;
}

// The region is drawn around the node rather than under it, so the inspector is where its name
// is spelled out in full.
// The words a content node carries, shown as its first detail because the title names its kind.
// Clipped: the whole of it is right below, under Source and Preview.
function contentRow(node: DisplayNode): DisplayNode["attributes"] {
    if (!titlesByKind(node)) return [];
    return [{ name: node.typeName!.toLowerCase(), value: ellipsize(node.label, MAX_TITLE_TEXT) }];
}

function regionSection(node: DisplayNode, tint: number | undefined): string {
    if (!node.region) return "";
    return (
        `<table><tbody><tr><th scope="row">region</th><td>` +
        `<button type="button" class="region-link" data-region="${escapeHtml(node.region)}">` +
        `${regionSwatch(tint)}${escapeHtml(node.region)}</button>` +
        `</td></tr></tbody></table>`
    );
}

function attributesTable(attributes: DisplayNode["attributes"]): string {
    if (!attributes.length) return "";
    const rows = attributes
        .map(
            (attr) =>
                `<tr><th scope="row">${escapeHtml(attr.name)}</th><td>${escapeHtml(attr.value)}</td></tr>`,
        )
        .join("");
    return `<table><tbody>${rows}</tbody></table>`;
}

/**
 * Where control comes from and where it goes, as two small tables.
 *
 * The drawing shows the same edges but cannot name them all at once; here the one node the reader
 * asked about says it in words. Every cell is a button, so reading the flow and walking it are the
 * same gesture: the node column takes you to that node, the edge column to that route.
 */
function neighborSections(neighbors: Neighbors | undefined): string {
    if (!neighbors) return "";
    return (
        neighborSection("Incoming", "Source", true, neighbors.incoming) +
        neighborSection("Outgoing", "Destination", false, neighbors.outgoing)
    );
}

function neighborSection(
    heading: string,
    endColumn: string,
    incoming: boolean,
    neighbors: Neighbor[],
): string {
    const rows = neighbors.length
        ? neighbors.map((neighbor) => neighborRow(incoming, neighbor)).join("")
        : `<tr><td class="neighbor-empty" colspan="2">None</td></tr>`;
    return (
        `<h4>${escapeHtml(heading)}</h4>` +
        `<table class="neighbors"><thead><tr>` +
        (incoming
            ? `<th scope="col">${escapeHtml(endColumn)}</th><th scope="col">Edge</th>`
            : `<th scope="col">Edge</th><th scope="col">${escapeHtml(endColumn)}</th>`) +
        `</tr></thead><tbody>${rows}</tbody></table>`
    );
}

/**
 * A region's border, named at both ends.
 *
 * A node's own tables can leave one end implied — it is the node you are looking at — but a
 * crossing has two nodes the reader does not yet know, so both are spelled out.
 */
function crossingSection(heading: string, crossings: BorderCrossing[]): string {
    const rows = crossings.length
        ? crossings.map(crossingRow).join("")
        : `<tr><td class="neighbor-empty" colspan="3">None</td></tr>`;
    return (
        `<h4>${escapeHtml(heading)}</h4>` +
        `<table class="neighbors crossings"><thead><tr>` +
        `<th scope="col">Source</th><th scope="col">Edge</th><th scope="col">Destination</th>` +
        `</tr></thead><tbody>${rows}</tbody></table>`
    );
}

function crossingRow(crossing: BorderCrossing): string {
    return (
        `<tr><td>${endCell(crossing.from)}</td>` +
        `<td>${edgeCell(crossing.from.id, crossing.to.id, crossing.category)}</td>` +
        `<td>${endCell(crossing.to)}</td></tr>`
    );
}

function endCell(end: CrossingEnd): string {
    return (
        `<button type="button" class="neighbor" data-node-id="${escapeHtml(end.id)}">` +
        `${categoryDot(end.category)}${cellText(end.label)}</button>`
    );
}

function neighborRow(incoming: boolean, neighbor: Neighbor): string {
    // An edge is named by the pair it joins, so a row can point at the same edge the drawing does.
    const [fromId, toId] = incoming
        ? [neighbor.id, neighbor.ownerId]
        : [neighbor.ownerId, neighbor.id];
    // Each row reads in the direction control travels: *that node, along this edge, to here* on
    // the way in; *from here, along this edge, to that node* on the way out.
    const node = `<td>${nodeCell(neighbor)}</td>`;
    const edge = `<td>${edgeCell(fromId, toId, neighbor.edgeCategory)}</td>`;
    return `<tr>${incoming ? node + edge : edge + node}</tr>`;
}

function nodeCell(neighbor: Neighbor): string {
    return (
        `<button type="button" class="neighbor" data-node-id="${escapeHtml(neighbor.id)}">` +
        `${categoryDot(neighbor.nodeCategory)}${cellText(neighbor.label)}</button>`
    );
}

function edgeCell(fromId: string, toId: string, category: string | undefined): string {
    const route = edgeStyle(category);
    if (!route || !category) return "";
    return (
        `<button type="button" class="route" data-from-id="${escapeHtml(fromId)}" ` +
        `data-to-id="${escapeHtml(toId)}">` +
        `<span class="route-swatch" style="background:${colorOf(category)}"></span>` +
        `${cellText(route.label)}</button>`
    );
}

/** The title HTML for a region's detail: its own band swatch beside the region's name. */
export function regionDetailTitle(region: RegionDetail): string {
    return regionSwatch(region.tint) + escapeHtml(region.name);
}

// A region is named in several places; each one wears the tint its band is drawn with, so the
// word and the color are never learned separately.
function regionSwatch(tint: number | undefined): string {
    return `<span class="region-swatch" data-tint="${tint ?? 0}"></span>`;
}

/**
 * The body HTML for a region's detail: how much it holds, what crosses its border, and its text.
 *
 * A scene is where a writer thinks in chapters, so the questions it answers are the chapter's:
 * how big is it, who can reach it, and where does it let you go.
 */
export function regionDetailBody(
    region: RegionDetail,
    source?: string,
    preview: NodePreviewOptions = {},
): string {
    const facts = [
        ...(region.kind ? [row("kind", region.kind)] : []),
        ...(region.anchor ? [row("anchor", region.anchor)] : []),
        row("nodes", String(region.nodeCount)),
    ].join("");
    const count = `<table><tbody>${facts}</tbody></table>`;
    const border =
        crossingSection("Entering", region.entering) + crossingSection("Leaving", region.leaving);
    const text =
        typeof source === "string"
            ? `<h4>Source</h4><pre><code>${escapeHtml(source)}</code></pre>` +
              `<h4>Preview</h4><div class="preview">${renderNodePreview(
                  source,
                  region.name,
                  preview.recognizeJumps ?? false,
              )}</div>`
            : "";
    return count + border + text;
}

/** The title HTML for an edge's detail: the route's own line swatch beside its name. */
export function edgeDetailTitle(category: string | undefined): string {
    const route = edgeStyle(category);
    const swatch = category
        ? `<span class="route-swatch" style="background:${colorOf(category)}"></span>`
        : "";
    return swatch + escapeHtml(route?.label ?? "Edge");
}

/**
 * The body HTML for an edge's detail: what this kind of route means, and the two nodes it joins.
 *
 * The ends are named Source and Destination — the same words the neighbor tables use — so a reader
 * moving between a node and an edge is never asked to translate.
 */
export function edgeDetailBody(edge: EdgeDetail): string {
    const meaning = edgeStyle(edge.category)?.meaning;
    const explanation = meaning ? `<p class="route-meaning">${escapeHtml(meaning)}</p>` : "";
    return (
        explanation +
        `<table class="neighbors"><tbody>` +
        endRow("Source", edge.source) +
        endRow("Destination", edge.target) +
        `</tbody></table>`
    );
}

function row(name: string, value: string): string {
    return `<tr><th scope="row">${escapeHtml(name)}</th><td>${escapeHtml(value)}</td></tr>`;
}

function endRow(name: string, end: EdgeEnd): string {
    return (
        `<tr><th scope="row">${escapeHtml(name)}</th><td>` +
        `<button type="button" class="neighbor" data-node-id="${escapeHtml(end.id)}">` +
        `${categoryDot(end.category)}${escapeHtml(end.label)}</button></td></tr>`
    );
}

function sourceSection(node: DisplayNode, preview: NodePreviewOptions = {}): string {
    const { source } = node;
    // A node with no source is synthetic — a stage inserted it (a filled default
    // speaker), so it maps to no text. Say so, instead of an empty Source block.
    if (typeof source !== "string") {
        return `<p class="inserted-note">Inserted by the compiler — no source of its own.</p>`;
    }
    return (
        `<h4>Source</h4><pre><code>${escapeHtml(source)}</code></pre>` +
        `<h4>Preview</h4><div class="preview">${renderNodePreview(
            source,
            node.label,
            preview.recognizeJumps ?? false,
        )}</div>`
    );
}
