import {
    create,
    pointer,
    stratify,
    tree,
    zoom,
    zoomIdentity,
    zoomTransform,
    type EnterElement,
    type HierarchyNode,
    type HierarchyPointLink,
    type HierarchyPointNode,
    type Selection,
} from "d3";
import type { DisplayEdge, DisplayNode, Stage } from "./model";
import type { CameraTransform } from "./graph-camera";
import { edgeStyle } from "./edge-style";
import {
    edgePath,
    labelBlockWidth,
    labelClearance,
    LABEL_BLOCK_ORIGIN,
    type Point,
} from "./edge-path";
import { bandsOf, type PlacedNode } from "./region-bands";
import { frameToFit, type Extent, type Insets } from "./fit-view";
import { colorOf } from "./palette";
import { ellipsize, MAX_INLINE_TEXT, tooltipHtml } from "./text";
import { createLegend } from "./legend";
import { createZoomControls, ZOOM_STEP, type ZoomControls } from "./zoom-controls";

/** A laid-out hierarchy node augmented with collapse state (`_children`). */
type TreeNode = HierarchyPointNode<DisplayNode> & {
    _children?: TreeNode[];
    children?: TreeNode[];
};

/**
 * The ids of a node's **lineage**: the node, all its ancestors (its path to the root), and
 * all its *visible* descendants (its subtree). Used to spotlight a node's place in the tree
 * on hover. It reads `children`, so a collapsed node's hidden descendants are excluded — the
 * highlight matches exactly what is drawn.
 */
export function lineageIds<T extends { id: string }>(node: HierarchyNode<T>): Set<string> {
    return new Set([...node.ancestors(), ...node.descendants()].map((member) => member.data.id));
}

const SCENE_NODE_RADIUS = 7;
const CONTENT_NODE_RADIUS = 5;

// The arrowhead's drawn size in user units, and how far its tip sits back from the node center so
// it stops exactly on the circle's edge: the node's radius plus the half of its stroke that sits
// outside the circle.
const ARROW_SIZE = 9;
const ARROW_STANDOFF = CONTENT_NODE_RADIUS + 0.75;

/**
 * The drawn size of a repeated glyph, and how far apart the line stamps them. The spacing is a
 * whole multiple of the dot pitch of the line it rides on, so the glyphs land on dots instead of
 * beating against them.
 */
const SYMBOL_SIZE = 7;
const SYMBOL_SPACING = 18;
/** How many dots ride between one glyph and the next. */
const DOTS_PER_SYMBOL = 3;

/** How far below the deepest row the first cross-link lane sits — one row's worth of clear air. */
const LANE_GAP = 62;

/** How far apart stacked cross-link lanes sit, so two routes never share a line. */
const LANE_STEP = 26;

/** How far apart the approach rows of routes ending at one node sit. */
const PORT_STEP = 9;

/** How finely a route is walked when deciding which one the pointer is nearest. */
const PICK_SAMPLE_SPACING = 12;
const PICK_SAMPLES = 160;

/** Where one cross-link travels: its own lane, its own corridor, its own approach row. */
interface CrossLinkTrack {
    lane: number;
    corridor: number;
    port: number;
}

/**
 * The horizontal distance between one depth and the next: room for a full-width label, plus the
 * gap the cross-link corridors climb in beside it.
 */
const COLUMN_STEP = 260;

/**
 * A scene-tree backbone node — a scene or the implicit document root. The Semantic tab
 * emphasizes these (a larger, thicker-ringed circle and bolder connecting edges) so the tab
 * reads as a scene tree with content hanging off it, not a flat node tree. Only that tab sets
 * a type name, so this is false everywhere else.
 */
function isSceneNode(node: DisplayNode): boolean {
    return node.typeName === "Scene" || node.typeName === "Document";
}

export interface TreeView {
    svg: SVGSVGElement;
    legend: HTMLElement;
    controls: HTMLElement;
    handleKey(event: KeyboardEvent): void;
    clearSelection(): void;
    /**
     * Select the node with the given stable {@link DisplayNode.id}, applying the same operation the
     * gesture would (an optional fold toggle and recenter), or return `false` when no node in this
     * view has that id. Used to resolve a deferred selection against the freshly installed view
     * after a save-triggered rebuild.
     */
    selectById(id: string, options?: NodeSelectOptions): boolean;
    /** Choose the route between two nodes, as clicking it in the drawing does. */
    selectEdgeBetween(fromId: string, toId: string): boolean;
    /** Choose a region, as clicking its band does. */
    selectRegion(region: string): void;
    /** Light up one node, or one route, while the pointer rests on a row naming it elsewhere. */
    spotlight(
        what: { nodeId?: string; fromId?: string; toId?: string; region?: string } | null,
    ): void;
    /**
     * Show the given camera and fold. A `null` camera uses the default (root-centered)
     * framing. Call after the tab becomes visible so the framing uses real dimensions.
     */
    applyView(camera: CameraTransform | null, fold: string[], zoom?: number | null): void;
}

/** Hooks that let the app remember and restore a graph's position across tabs. */
export interface TreeViewOptions {
    /**
     * The camera to apply on creation: a pinned override, the inherited shared
     * camera, or `null` for the default (root-centered) framing.
     */
    initialCamera?: CameraTransform | null;
    /** The collapsed node ids to restore on creation. */
    initialFold?: string[];
    /** The scale to open at when there is no pinned camera — inherited, not the default. */
    initialZoom?: number | null;
    /**
     * Whether a node can be folded away.
     *
     * A tree's children *are* its content, so hiding them hides only detail. A graph's are an
     * accident of which route happened to reach them first: folding one takes away nodes other
     * routes still lead to, and the edges into them, leaving a picture of the flow that is not
     * true. A graph stage therefore says no.
     */
    foldable?: boolean;
    /**
     * Fired when the camera changes; `byUser` is true for reader gestures (wheel,
     * drag, the zoom controls) and false for programmatic applies (a reveal, the
     * default framing).
     */
    onCameraChange?(transform: CameraTransform, byUser: boolean): void;
    /** Fired when the reader collapses or expands a node. */
    onFoldChange?(collapsed: string[]): void;
    /** Fired when the reader clicks an edge, so the caller can show the route it is. */
    onSelectEdge?(edge: DisplayEdge): void;
    /** Fired when the reader clicks a region band, so the caller can show the region it is. */
    onSelectRegion?(region: string): void;
    /** Fired when the reader clicks Revert, so the caller can drop remembered state. */
    onRevert?(): void;
}

/** The operation a node gesture carries alongside selection: an optional fold toggle and recenter. */
export interface NodeSelectOptions {
    /** Toggle the node's fold (collapse/expand) as a click on its circle does. */
    toggle?: boolean;
    /** Recenter the camera on the node, as keyboard navigation does. */
    center?: boolean;
    /**
     * Open whatever is folded over the node, so it is actually on screen. For deliberate
     * navigation — a search hit, a neighbor row — where landing on a hidden node would look like
     * nothing happened. Restoring a selection after a rebuild leaves it off, so a fold the reader
     * closed stays closed.
     */
    reveal?: boolean;
}

const NAVIGATION_KEYS = ["ArrowRight", "ArrowLeft", "ArrowUp", "ArrowDown", "Enter", " "];

// Default framing: a readable 100% zoom with the root anchored near the left edge and
// vertically centered, so the reader starts at the root with its subtree filling the
// viewport rightward — rather than a whole-graph fit that shrinks large trees.
const DEFAULT_ZOOM = 1;
const ROOT_ANCHOR_X = 0.2;

/** Air between the drawing and a panel floating over the canvas. */
const FLOATING_PANEL_GAP = 12;

/** How far out the reader may zoom by hand. */
const MIN_ZOOM = 0.03;

/**
 * The smallest scale a stage will *open* at.
 *
 * Fitting a long script would shrink it to an unreadable smudge, so a stage that cannot be shown
 * whole at this scale opens at the start of itself instead. The reader can still zoom further out
 * than this — a default just should not choose it for them.
 */
const LEGIBLE_ZOOM = 0.15;

/** Render one stage as an interactive, collapsible D3 tree with legend + zoom.
 *  `options` supply the initial camera/fold and hooks so the app can remember a
 *  graph's position across tab switches and hot-reloads. */
export function createTreeView(
    stage: Stage,
    onSelect: (node: DisplayNode) => void,
    options: TreeViewOptions = {},
): TreeView {
    const {
        initialCamera = null,
        initialFold = [],
        initialZoom = null,
        foldable = true,
        onCameraChange,
        onFoldChange,
        onSelectEdge,
        onSelectRegion,
        onRevert,
    } = options;
    const referenceEdges = stage.edges.filter((edge) => edge.kind === "Reference");

    // A stage whose edges mean different things colors them; the lookup is by the pair they join,
    // which is unique because a node is reached from a given node at most once.
    const edgeCategories = new Map(
        stage.edges
            .filter((edge) => edge.category)
            .map((edge) => [`${edge.fromId}->${edge.toId}`, edge.category!]),
    );
    const categoryOfLink = (fromId: string, toId: string): string | undefined =>
        edgeCategories.get(`${fromId}->${toId}`);
    const edgeCategoriesPresent = [...new Set(edgeCategories.values())];
    const root = buildHierarchy(stage);
    root.each((node) => {
        (node as TreeNode)._children = (node as TreeNode).children;
    });

    let selected: TreeNode | null = null;
    const dimmed = new Set<string>();
    // The reader's current object, when it is not a node. Exactly one of these three is set at a
    // time: choosing a route or a region is as much a choice as choosing a node, and the drawing
    // says so by letting the last one go.
    let selectedEdge: { fromId: string; toId: string } | null = null;
    let selectedRegion: string | null = null;
    // The node whose lineage is currently spotlighted on hover (null when not hovering).
    let focused: TreeNode | null = null;
    // Set while a control-driven (user) zoom is applied, so the zoom handler can tell
    // reader gestures from programmatic applies even when both lack a DOM sourceEvent.
    let userGesture = false;
    // Bumped by every applyView so a stale async default-framing retry (scheduled by an
    // earlier applyView, e.g. this tab's hidden construction) aborts instead of clobbering
    // a camera a later applyView (e.g. the reveal) has since applied.
    let viewToken = 0;
    // The scale an untouched graph opens at, inherited from wherever the reader was. Only the
    // scale travels between graphs; where they are looking does not.
    let inheritedZoom: number | null = null;

    const svg = create<SVGSVGElement>("svg").attr("class", "tree");

    // Flow reads one way, so a stage whose edges mean something points them. A marker cannot
    // inherit its line's color, so each category gets its own, and the id is namespaced per stage
    // because several stages share one document.
    const markerScope = stage.title.replace(/\W+/g, "-").toLowerCase();
    const arrowFor = (category: string | undefined): string | null =>
        category && edgeStyle(category)?.isRoute ? `url(#arrow-${markerScope}-${category})` : null;
    const symbolFor = (category: string | undefined): string | null =>
        category && edgeStyle(category)?.symbol ? `url(#tick-${markerScope}-${category})` : null;
    if (edgeCategoriesPresent.length > 0) {
        const defs = svg.append("defs");
        for (const category of edgeCategoriesPresent.filter((c) => edgeStyle(c)?.symbol)) {
            defs.append("marker")
                .attr("id", `tick-${markerScope}-${category}`)
                .attr("viewBox", "0 0 10 10")
                .attr("refX", 5)
                .attr("refY", 5)
                .attr("markerWidth", SYMBOL_SIZE)
                .attr("markerHeight", SYMBOL_SIZE)
                .attr("markerUnits", "userSpaceOnUse")
                // Upright rather than along the line: a cross reads as a cross only when it is
                // not rotated into a plus by a vertical stretch of route.
                .attr("orient", "0")
                .append("path")
                .attr("d", "M 2 2 L 8 8 M 8 2 L 2 8")
                .attr("stroke", colorOf(category))
                .attr("stroke-width", 1.6)
                .attr("fill", "none");
        }
        for (const category of edgeCategoriesPresent.filter((c) => edgeStyle(c)?.isRoute)) {
            defs.append("marker")
                .attr("id", `arrow-${markerScope}-${category}`)
                .attr("viewBox", "0 0 10 10")
                // A link ends at its target's center, so the head is pushed back to the circle's
                // edge — otherwise the arrow is drawn underneath the dot and never seen. refX is
                // in viewBox units, which the marker scales to ARROW_SIZE across.
                .attr("refX", 10 + (ARROW_STANDOFF * 10) / ARROW_SIZE)
                .attr("refY", 5)
                .attr("markerWidth", ARROW_SIZE)
                .attr("markerHeight", ARROW_SIZE)
                .attr("markerUnits", "userSpaceOnUse")
                .attr("orient", "auto-start-reverse")
                .append("path")
                .attr("d", "M 0 0 L 10 5 L 0 10 z")
                .attr("fill", colorOf(category));
        }
    }

    const viewport = svg.append("g");
    // Regions are the ground the drawing stands on, so they are laid down before anything else.
    const gRegions = viewport.append("g").attr("class", "regions");
    const gLinks = viewport.append("g");
    const gReferences = viewport.append("g");
    const gNodes = viewport.append("g");
    // A node's click target is a generous rectangle that reaches into the corridor its own
    // outgoing edges travel through, so it would swallow every hover aimed at a line. The lines
    // therefore get an invisible, wider twin above the nodes: on the stroke, the edge wins.
    const gEdgeHits = viewport.append("g").attr("class", "edge-hits");

    const zoomBehavior = zoom<SVGSVGElement, undefined>()
        // The floor is low enough that even a long script fits on arrival; a reader who wants to
        // read rather than survey zooms straight back in.
        .scaleExtent([MIN_ZOOM, 3])
        // Use the container size as the extent so zoom centers correctly and does not
        // depend on the SVG's intrinsic size.
        .extent(() => {
            const size = viewportSize();
            return [
                [0, 0],
                [size.width, size.height],
            ] as [[number, number], [number, number]];
        })
        .on("zoom", (event) => {
            viewport.attr("transform", event.transform.toString());
            controls.setRatio(event.transform.k);
            const byUser = userGesture || Boolean(event.sourceEvent);
            const { k, x, y } = event.transform;
            onCameraChange?.({ k, x, y }, byUser);
        });
    svg.call(zoomBehavior);

    const controls: ZoomControls = createZoomControls({
        onZoomIn: () => userAction(() => svg.call(zoomBehavior.scaleBy, ZOOM_STEP)),
        onZoomOut: () => userAction(() => svg.call(zoomBehavior.scaleBy, 1 / ZOOM_STEP)),
        onSetZoom: (percent) =>
            userAction(() => svg.call(zoomBehavior.scaleTo, clampScale(percent / 100))),
        onRevert: () => revert(),
    });

    const legend = createLegend(stage, {
        onToggle: (category, isDimmed) => {
            if (isDimmed) dimmed.add(category);
            else dimmed.delete(category);
            applyCategoryFilter();
        },
        onHover: (category) => highlightCategory(category),
        onLeave: () => clearHighlight(),
    });

    // The column step is wide enough for a full-width label plus the lead-out its outgoing line
    // needs, so a node's own words never run into the next column or get struck through.
    const layout = tree<DisplayNode>().nodeSize([62, COLUMN_STEP]);
    // The tree lays out depth along y and rows along x; the drawing reads the other way round.
    const at = (node: { x: number; y: number }): Point => ({ x: node.y, y: node.x });

    applyView(initialCamera, initialFold, initialZoom);

    return {
        svg: svg.node()!,
        legend,
        controls: controls.element,
        handleKey,
        clearSelection: () => {
            selected = null;
            selectedEdge = null;
            selectedRegion = null;
            applySelection();
        },
        selectById,
        selectEdgeBetween: (fromId, toId) => {
            const edge = edgeBetween(fromId, toId);
            if (edge) selectEdge(edge);
            return Boolean(edge);
        },
        selectRegion,
        spotlight,
        applyView,
    };

    /**
     * Lights up whatever a row elsewhere is naming — a node, or a route — so the reader can see
     * that the words and the drawing are the same thing seen twice.
     */
    function spotlight(
        what: { nodeId?: string; fromId?: string; toId?: string; region?: string } | null,
    ): void {
        gRegions
            .selectAll<SVGGElement, { region: string }>("g.region")
            .classed("highlight", (datum) => datum.region === what?.region);
        gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .classed(
                "highlight",
                (d) =>
                    (Boolean(what?.nodeId) && d.data.id === what?.nodeId) ||
                    (Boolean(what?.region) && d.data.region === what?.region),
            );
        viewport
            .selectAll<SVGPathElement, unknown>("path.link")
            .nodes()
            .forEach((link) =>
                link.classList.toggle(
                    "highlight",
                    Boolean(what?.fromId) &&
                        link.dataset.fromId === what?.fromId &&
                        link.dataset.toId === what?.toId,
                ),
            );
    }

    // Names the route a line is, so hovering it says what it means and a screen reader can read
    // it. The class is what the stylesheet thickens on hover — an edge is thin, so it needs a
    // generous target and a clear response.
    function describeEdge<Datum>(
        selection: Selection<SVGPathElement, Datum, SVGGElement, unknown>,
        categoryOfDatum: (datum: Datum) => string | undefined,
        endsOfDatum: (datum: Datum) => { fromId: string; toId: string },
    ): void {
        selection
            .classed("routed", (datum) => Boolean(edgeStyle(categoryOfDatum(datum))))
            .each(function (datum) {
                const ends = endsOfDatum(datum);
                this.dataset.fromId = ends.fromId;
                this.dataset.toId = ends.toId;
                // A fresh title each join, so a rebuilt edge never keeps a stale name.
                this.replaceChildren();
                const category = categoryOfDatum(datum);
                const style = edgeStyle(category);
                delete this.dataset.cursor;
                delete this.dataset.category;
                if (!style) return;
                this.dataset.cursor = style.cursor;
                this.dataset.category = category;
                const title = document.createElementNS("http://www.w3.org/2000/svg", "title");
                title.textContent = style.label;
                this.appendChild(title);
            });
    }

    /* --- hierarchy --- */

    /**
     * Measures every drawn node and sizes its click target to match, returning how far past each
     * node's dot an outgoing line must start to clear its own words.
     *
     * The text is measured rather than estimated: a character-count guess is wrong by exactly the
     * amount that puts a line through a label. A DOM that does not lay text out (a test's) has
     * nothing to measure, and reports zero — the lines are then merely shorter, never misplaced.
     */
    function measureLabelBlocks(): Map<string, { clearance: number; width: number }> {
        const measured = new Map<string, { clearance: number; width: number }>();
        gNodes.selectAll<SVGGElement, TreeNode>("g.node").each(function (datum) {
            const widest = [...this.querySelectorAll<SVGTextElement>("text")].reduce(
                (width, text) => Math.max(width, text.getComputedTextLength?.() ?? 0),
                0,
            );
            const width = labelBlockWidth(widest);
            measured.set(datum.data.id, { clearance: labelClearance(widest), width });
            this.querySelector("rect.hit")?.setAttribute("width", String(width));
        });
        return measured;
    }

    /**
     * Draws each region as a band behind the nodes that share it.
     *
     * A scene names an area of the document, so it is written once around that area rather than
     * repeated under every line inside it — which is both quieter and one line shorter per node.
     */
    function drawRegions(
        nodes: readonly TreeNode[],
        measured: Map<string, { width: number }>,
    ): void {
        const placed: PlacedNode[] = nodes.map((node) => ({
            region: node.data.region,
            x: node.y,
            y: node.x,
            width: measured.get(node.data.id)?.width ?? 0,
        }));

        const band = gRegions
            .selectAll<SVGGElement, ReturnType<typeof bandsOf>[number]>("g.region")
            .data(bandsOf(placed), (datum) => datum.region);
        band.exit().remove();
        const entering = band.enter().append("g").attr("class", "region");
        entering
            .append("rect")
            // A region is a thing a reader can ask about, so the band it is drawn as answers.
            .on("click", (_event, datum) => selectRegion(datum.region));
        entering.append("text").attr("class", "region-name");

        const all = gRegions.selectAll<SVGGElement, ReturnType<typeof bandsOf>[number]>("g.region");
        all.attr("data-tint", (datum) => datum.tint);
        all.select("rect")
            .attr("x", (datum) => datum.x)
            .attr("y", (datum) => datum.y)
            .attr("width", (datum) => datum.width)
            .attr("height", (datum) => datum.height)
            .attr("rx", 10);
        all.select("text")
            .attr("x", (datum) => datum.x + 12)
            .attr("y", (datum) => datum.y + 17)
            .text((datum) => datum.region);
    }

    // The empty band under the deepest row, where cross-links travel. Each cross-link gets a lane
    // of its own so two never share a line: the shortest hop runs closest to the drawing and a
    // longer one passes beneath it, the way nested brackets never cross.
    function assignLanes(
        edges: readonly DisplayEdge[],
        positionById: Map<string, TreeNode>,
        nodes: readonly TreeNode[],
    ): Map<string, CrossLinkTrack> {
        const floor = nodes.reduce((low, node) => Math.max(low, node.x), 0) + LANE_GAP;
        const spanOf = (edge: DisplayEdge): number =>
            Math.abs(positionById.get(edge.toId)!.y - positionById.get(edge.fromId)!.y);
        // Routes ending at one node queue up: each takes the next corridor back from that node's
        // column, and leans in from its own row, so none of them lies on top of another.
        const arrivals = new Map<string, number>();
        return new Map(
            [...edges]
                .sort((left, right) => spanOf(left) - spanOf(right))
                .map((edge, depth) => {
                    const queued = arrivals.get(edge.toId) ?? 0;
                    arrivals.set(edge.toId, queued + 1);
                    return [
                        edgeKey(edge),
                        {
                            lane: floor + depth * LANE_STEP,
                            corridor: queued,
                            port: portOffset(queued),
                        },
                    ] as const;
                }),
        );
    }

    // Ports fan either side of the target's own row — 0, above, below, further above — so the
    // first route arrives dead level and the rest lean off it symmetrically.
    function portOffset(queued: number): number {
        const rank = Math.ceil(queued / 2);
        return queued === 0 ? 0 : (queued % 2 === 1 ? -1 : 1) * rank * PORT_STEP;
    }

    function edgeKey(edge: DisplayEdge): string {
        return `${edge.fromId}->${edge.toId}`;
    }

    function edgeBetween(fromId?: string, toId?: string): DisplayEdge | undefined {
        return stage.edges.find((edge) => edge.fromId === fromId && edge.toId === toId);
    }

    function clearHovered(): void {
        viewport
            .selectAll<SVGPathElement, unknown>("path.link.hovered")
            .nodes()
            .forEach((link) => link.classList.remove("hovered"));
    }

    /**
     * The route actually closest to the pointer, rather than whichever twin happens to be on top.
     *
     * A route's pointer target is deliberately wider than its line, so where two run close
     * together their targets overlap and the topmost one wins by accident of draw order. Measuring
     * makes the answer the one the reader was aiming at.
     */
    function nearestTo(event: PointerEvent | MouseEvent, fallback: SVGPathElement): SVGPathElement {
        const twins = gEdgeHits.selectAll<SVGPathElement, SVGPathElement>("path.edge-hit").data();
        if (twins.length < 2 || !fallback.getPointAtLength) return fallback;
        const [x, y] = pointer(event, viewport.node()!);
        let best = fallback;
        let bestDistance = Infinity;
        for (const route of twins) {
            const distance = distanceToPath(route, x, y);
            if (distance < bestDistance) {
                bestDistance = distance;
                best = route;
            }
        }
        return best;
    }

    /** How far a point lies from a path, measured by walking it — near enough, and exact enough. */
    function distanceToPath(path: SVGPathElement, x: number, y: number): number {
        const length = path.getTotalLength();
        const steps = Math.max(2, Math.min(PICK_SAMPLES, Math.round(length / PICK_SAMPLE_SPACING)));
        let nearest = Infinity;
        for (let step = 0; step <= steps; step++) {
            const point = path.getPointAtLength((length * step) / steps);
            nearest = Math.min(nearest, (point.x - x) ** 2 + (point.y - y) ** 2);
        }
        return nearest;
    }

    function trim(value: number): number {
        return Math.round(value * 100) / 100;
    }

    /**
     * Rewrites a symbol-carrying line as an evenly sampled polyline, so its `marker-mid` glyph
     * lands at regular intervals along it.
     *
     * SVG stamps a marker at a path's vertices, and a curve has only its two ends — so a repeated
     * glyph needs vertices to stand on. The samples are close enough that the polyline is
     * indistinguishable from the curve it replaces. A DOM that measures nothing (a test's) leaves
     * the curve as it is.
     */
    function stampSymbols(path: SVGPathElement, category: string | undefined): void {
        if (!edgeStyle(category)?.symbol || !path.getTotalLength) return;
        const length = path.getTotalLength();
        const steps = Math.max(2, Math.round(length / SYMBOL_SPACING));
        const points = Array.from({ length: steps + 1 }, (_, step) =>
            path.getPointAtLength((length * step) / steps),
        );
        // The dots between the glyphs take their pitch from the glyphs' own spacing, so the two
        // patterns stay in step instead of drifting against each other along the line.
        path.style.strokeDasharray = `0 ${trim(length / steps / DOTS_PER_SYMBOL)}`;
        path.setAttribute(
            "d",
            points
                .map(
                    (point, index) => `${index === 0 ? "M" : "L"}${trim(point.x)},${trim(point.y)}`,
                )
                .join(""),
        );
    }

    function buildHierarchy(stage: Stage): TreeNode {
        const parentOf = new Map<string, string>();
        for (const edge of stage.edges) {
            if (edge.kind !== "Reference") parentOf.set(edge.toId, edge.fromId);
        }
        return stratify<DisplayNode>()
            .id((node) => node.id)
            .parentId((node) => parentOf.get(node.id) ?? null)(stage.nodes) as unknown as TreeNode;
    }

    /* --- selection, filter, highlight --- */

    function select(node: TreeNode): void {
        selected = node;
        selectedEdge = null;
        selectedRegion = null;
        applySelection();
        onSelect(node.data);
    }

    // Apply a node's full click/keyboard operation: an optional fold toggle, the selection, and an
    // optional recenter — so a deferred selection reproduces exactly what the original gesture
    // would have done, on whichever view is current.
    function applyNodeSelection(node: TreeNode, options: NodeSelectOptions): void {
        if (options.toggle) toggle(node);
        select(node);
        if (options.center) centerOn(node);
    }

    // Resolve a node by its stable id against this view's current hierarchy (including collapsed
    // subtrees) and apply the operation, or report failure so a deferred selection cancels safely.
    // This is how a selection deferred across a save-triggered rebuild lands on the freshly
    // installed node — with its current source spans — rather than the stale node the click
    // captured.
    function selectById(id: string, options: NodeSelectOptions = {}): boolean {
        const node = findNodeById(id);
        if (node === null) return false;
        if (options.reveal) unfoldOver(node);
        applyNodeSelection(node, options);
        return true;
    }

    /**
     * Opens whatever is folded over a node, so selecting it actually shows it.
     *
     * A node reached by name — from a search, or from a neighbor row — may sit inside a collapsed
     * branch. Selecting it there would mark a node that is nowhere on screen: the inspector would
     * fill in and the drawing would not move.
     */
    function unfoldOver(node: TreeNode): void {
        const folded = node
            .ancestors()
            .filter((ancestor) => !ancestor.children && ancestor._children) as TreeNode[];
        if (folded.length === 0) return;
        for (const ancestor of folded) ancestor.children = ancestor._children;
        update();
        onFoldChange?.(collapsedIds());
    }

    function findNodeById(id: string): TreeNode | null {
        const stack: TreeNode[] = [root];
        while (stack.length > 0) {
            const node = stack.pop()!;
            if (node.data.id === id) return node;
            const children = (node.children ?? node._children) as TreeNode[] | undefined;
            if (children) stack.push(...children);
        }
        return null;
    }

    // Selecting a different node applies immediately: the inspector is read-only, so a selection
    // can never leave unsaved work behind.
    function guardSelect(node: TreeNode, options: NodeSelectOptions): void {
        applyNodeSelection(node, options);
    }

    function applySelection(): void {
        gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .classed("selected", (d) => d === selected);
        viewport
            .selectAll<SVGPathElement, unknown>("path.link")
            .nodes()
            .forEach((link) =>
                link.classList.toggle(
                    "selected",
                    link.dataset.fromId === selectedEdge?.fromId &&
                        link.dataset.toId === selectedEdge?.toId,
                ),
            );
        gRegions
            .selectAll<SVGGElement, { region: string }>("g.region")
            .classed("selected", (datum) => datum.region === selectedRegion);
    }

    /** Make this route the reader's current object, letting go of whatever was chosen before. */
    function selectEdge(edge: DisplayEdge): void {
        selected = null;
        selectedRegion = null;
        selectedEdge = { fromId: edge.fromId, toId: edge.toId };
        applySelection();
        onSelectEdge?.(edge);
    }

    /** Make this region the reader's current object, letting go of whatever was chosen before. */
    function selectRegion(region: string): void {
        selected = null;
        selectedEdge = null;
        selectedRegion = region;
        applySelection();
        onSelectRegion?.(region);
    }

    // Nodes and routes answer the legend the same way. Their category names never collide — a
    // stage's nodes and its edges are drawn from disjoint parts of the palette — so one dimmed set
    // serves both.
    function applyCategoryFilter(): void {
        gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .classed("dimmed", (d) =>
                Boolean(
                    (d.data.category && dimmed.has(d.data.category)) ||
                    (d.data.region && dimmed.has(d.data.region)),
                ),
            );
        gRegions
            .selectAll<SVGGElement, { region: string }>("g.region")
            .classed("dimmed", (datum) => dimmed.has(datum.region));
        eachLink((link, category) =>
            link.classList.toggle("dimmed", Boolean(category && dimmed.has(category))),
        );
    }

    function highlightCategory(category: string): void {
        gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .classed(
                "highlight",
                (d) => d.data.category === category || d.data.region === category,
            );
        eachLink((link, own) => link.classList.toggle("highlight", own === category));
        gRegions
            .selectAll<SVGGElement, { region: string }>("g.region")
            .classed("highlight", (datum) => datum.region === category);
    }

    function clearHighlight(): void {
        gNodes.selectAll<SVGGElement, TreeNode>("g.node").classed("highlight", false);
        eachLink((link) => link.classList.remove("highlight"));
        gRegions.selectAll<SVGGElement, { region: string }>("g.region").classed("highlight", false);
    }

    function eachLink(apply: (link: SVGPathElement, category?: string) => void): void {
        viewport
            .selectAll<SVGPathElement, unknown>("path.link")
            .nodes()
            .forEach((link) => apply(link, link.dataset.category));
    }

    /* --- lineage focus (hover) --- */

    function setFocus(node: TreeNode | null): void {
        focused = node;
        applyFocus();
    }

    // Spotlight the hovered node's lineage: mark its nodes and the edges between them
    // `.related` and flag the svg `.has-focus`, so CSS fades everything else. Re-run after
    // every update() so entering nodes/links inherit the current focus. Class-only, so it
    // composes with the scene backbone (stroke width) and the selection/category states.
    function applyFocus(): void {
        const related = focused ? lineageIds(focused) : null;
        svg.classed("has-focus", related !== null);
        gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .classed("related", (d) => related?.has(d.data.id) ?? false);
        const linked = (source: TreeNode, target: TreeNode): boolean =>
            related !== null && related.has(source.data.id) && related.has(target.data.id);
        gLinks
            .selectAll<SVGPathElement, HierarchyPointLink<DisplayNode>>("path.link")
            .classed("related", (link) => linked(link.source as TreeNode, link.target as TreeNode));
        gReferences
            .selectAll<SVGPathElement, DisplayEdge>("path.reference")
            .classed(
                "related",
                (edge) => related !== null && related.has(edge.fromId) && related.has(edge.toId),
            );
    }

    /* --- collapse / expand --- */

    function toggle(node: TreeNode): void {
        if (!foldable) return;
        node.children = node.children ? undefined : node._children;
        update();
        onFoldChange?.(collapsedIds());
    }

    function expand(node: TreeNode): void {
        if (!foldable) return;
        if (!node.children && node._children) {
            node.children = node._children;
            update();
            onFoldChange?.(collapsedIds());
        }
    }

    /* --- keyboard navigation --- */

    function handleKey(event: KeyboardEvent): void {
        if (!NAVIGATION_KEYS.includes(event.key)) return;
        event.preventDefault();

        if (!selected) {
            select(root);
            scheduleDefaultView(++viewToken);
            return;
        }
        if (event.key === "Enter" || event.key === " ") {
            toggle(selected);
            applySelection();
            return;
        }
        const next = nextNode(event.key, selected);
        if (next) {
            guardSelect(next, { center: true });
        }
    }

    function nextNode(key: string, node: TreeNode): TreeNode | null {
        if (key === "ArrowRight") {
            expand(node);
            return node.children ? node.children[0] : null;
        }
        if (key === "ArrowLeft") return (node.parent as TreeNode | null) ?? null;
        if (key === "ArrowDown") return sibling(node, 1);
        if (key === "ArrowUp") return sibling(node, -1);
        return null;
    }

    function sibling(node: TreeNode, offset: number): TreeNode | null {
        const siblings = node.parent?.children as TreeNode[] | undefined;
        if (!siblings) return null;
        return siblings[siblings.indexOf(node) + offset] ?? null;
    }

    /* --- rendering --- */

    function update(): void {
        layout(root);
        const nodes = root.descendants() as TreeNode[];
        const positionById = new Map(nodes.map((node) => [node.data.id, node]));

        // Nodes are placed and measured before any line is drawn, because a line's shape depends
        // on how wide the words it leaves behind actually are. Paint order is unaffected: the
        // link, reference, and node layers were appended once, in that order.
        const node = gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .data(nodes, (datum) => datum.data.id);
        node.exit().remove();
        appendEnteringNodes(node.enter());

        gNodes
            .selectAll<SVGGElement, TreeNode>("g.node")
            .attr("transform", (d) => `translate(${d.y},${d.x})`)
            .classed("collapsed", (d) => !d.children && Boolean(d._children));

        const measured = measureLabelBlocks();
        const clearanceOf = (id: string): number => measured.get(id)?.clearance ?? 0;
        drawRegions(nodes, measured);

        const categoryOf = (link: HierarchyPointLink<DisplayNode>): string | undefined =>
            categoryOfLink((link.source as TreeNode).data.id, (link.target as TreeNode).data.id);
        const categoryColor = (link: HierarchyPointLink<DisplayNode>): string | null => {
            const category = categoryOf(link);
            return category ? colorOf(category) : null;
        };
        const dashOfLink = (link: HierarchyPointLink<DisplayNode>): string | null =>
            edgeStyle(categoryOf(link))?.dash ?? null;

        gLinks
            .selectAll<SVGPathElement, HierarchyPointLink<DisplayNode>>("path.link")
            .data(root.links(), (link) => (link.target as TreeNode).data.id)
            .join("path")
            .attr("class", "link")
            // A link into a scene is part of the scene backbone (root→scene, scene→subscene);
            // emphasize it over the edges to a scene's content blocks.
            .classed("scene", (link) => (link.target as TreeNode).data.typeName === "Scene")
            // A style, not an attribute: the stylesheet's own `.link` stroke would win over one.
            .style("stroke", (link) => categoryColor(link))
            .style("stroke-dasharray", (link) => dashOfLink(link))
            .attr("marker-end", (link) => arrowFor(categoryOf(link)))
            .attr("marker-mid", (link) => symbolFor(categoryOf(link)))
            .call(
                describeEdge,
                (link: HierarchyPointLink<DisplayNode>) => categoryOf(link),
                (link: HierarchyPointLink<DisplayNode>) => ({
                    fromId: (link.source as TreeNode).data.id,
                    toId: (link.target as TreeNode).data.id,
                }),
            )
            .attr("d", (link) =>
                edgePath(at(link.source), at(link.target), {
                    clearance: clearanceOf((link.source as TreeNode).data.id),
                }),
            )
            .each(function (link) {
                stampSymbols(this, categoryOf(link));
            });

        const crossLinks = referenceEdges.filter(
            (edge) => positionById.has(edge.fromId) && positionById.has(edge.toId),
        );
        const laneOf = assignLanes(crossLinks, positionById, nodes);

        gReferences
            .selectAll<SVGPathElement, DisplayEdge>("path.reference")
            .data(crossLinks, (edge) => edgeKey(edge))
            .join("path")
            .attr("class", "link reference")
            .style("stroke", (edge) => (edge.category ? colorOf(edge.category) : null))
            // A categorized line owns its pattern outright, so a plain succession that happens to
            // be drawn as a cross-link stays solid rather than inheriting the reference dash.
            .style("stroke-dasharray", (edge) =>
                edge.category ? (edgeStyle(edge.category)?.dash ?? "none") : null,
            )
            .attr("marker-end", (edge) => arrowFor(edge.category))
            .attr("marker-mid", (edge) => symbolFor(edge.category))
            .call(
                describeEdge,
                (edge: DisplayEdge) => edge.category,
                (edge: DisplayEdge) => ({ fromId: edge.fromId, toId: edge.toId }),
            )
            // A cross-link spans the drawing rather than one step of it, so it travels its own
            // lane below every row instead of lying across the words in between.
            .attr("d", (edge) =>
                edgePath(at(positionById.get(edge.fromId)!), at(positionById.get(edge.toId)!), {
                    clearance: clearanceOf(edge.fromId),
                    ...(laneOf.get(edgeKey(edge)) ?? {}),
                }),
            )
            .each(function (edge) {
                stampSymbols(this, edge.category);
            });

        applySelection();
        applyCategoryFilter();
        applyFocus();
        syncEdgeHits();
    }

    // Mirrors every named route as an invisible wide path above the nodes, and lends its hover to
    // the real line. Kept in sync after each render rather than joined on its own data, so the
    // twin can never disagree with the line it stands for.
    function syncEdgeHits(): void {
        const routes = viewport.selectAll<SVGPathElement, unknown>("path.link.routed").nodes();

        gEdgeHits
            .selectAll<SVGPathElement, SVGPathElement>("path.edge-hit")
            .data(routes)
            .join("path")
            .attr("class", "edge-hit")
            .attr("d", (route) => route.getAttribute("d"))
            // The pointer names the route before the tooltip does — a question mark over a
            // conditional, a hand over a choice arm, the barred circle over what is never reached.
            .style("cursor", (route) => route.dataset.cursor ?? null)
            .each(function (route) {
                this.replaceChildren();
                const title = route.querySelector("title");
                if (title) this.appendChild(title.cloneNode(true));
            })
            .on("mouseenter", (event, route) => nearestTo(event, route).classList.add("hovered"))
            .on("mouseleave", () => clearHovered())
            .on("click", (event, route) => {
                const picked = nearestTo(event, route);
                const edge = edgeBetween(picked.dataset.fromId, picked.dataset.toId);
                // A placement link is a visual clue, not flow: there is no route to open.
                if (edge && edgeStyle(edge.category)?.isRoute) selectEdge(edge);
            });
    }

    function appendEnteringNodes(
        enter: Selection<EnterElement, TreeNode, SVGGElement, unknown>,
    ): void {
        const group = enter
            .append("g")
            .attr("class", "node")
            // A scene or the document root gets the emphasized scene-backbone styling.
            .classed("scene", (d) => isSceneNode(d.data))
            .attr("data-tip", (d) => tooltipHtml(d.data))
            // A cross-linked node carries a scene/speaker key so the entity highlighter can
            // light it up with the matching table rows: a scene node *is* the entity
            // (entityKey), a jump or speaker mention *references* one (refKey). Absent elsewhere.
            .attr("data-entity-key", (d) => d.data.entityKey ?? null)
            .attr("data-ref-key", (d) => d.data.refKey ?? null)
            // Spotlight this node's lineage while the pointer is over it. mouseenter/leave
            // (not over/out) fire once per node, so moving within the node does not re-trigger.
            .on("mouseenter", (_event, d) => setFocus(d))
            .on("mouseleave", () => setFocus(null));

        group
            .append("circle")
            .attr("r", (d) => (isSceneNode(d.data) ? SCENE_NODE_RADIUS : CONTENT_NODE_RADIUS))
            .style("fill", (d) => colorOf(d.data.category))
            .on("click", (_event, d) => {
                guardSelect(d, { toggle: foldable });
            });

        group
            .append("text")
            .attr("class", "label")
            .attr("dy", "0.32em")
            .attr("x", 12)
            // Clipped to the same width as the attributes beneath it. A line of dialogue can run
            // longer than the column it sits in, and an unclipped label overprints its neighbor;
            // the inspector and the hover tip carry the whole of it.
            .text((d) => ellipsize(d.data.label, MAX_INLINE_TEXT));

        group.each(function (d) {
            d.data.attributes.forEach((attr, i) => {
                const text = document.createElementNS("http://www.w3.org/2000/svg", "text");
                text.setAttribute("class", "attr");
                text.setAttribute("x", "12");
                text.setAttribute("dy", String(15 + i * 12));
                text.textContent = ellipsize(`${attr.name}: ${attr.value}`, MAX_INLINE_TEXT);
                this.appendChild(text);
            });
        });

        // A generous transparent hit area behind the label and attributes, so the whole node block
        // is clickable to inspect. Its width is set from the measured text once the node is drawn.
        group.each(function (d) {
            const rect = document.createElementNS("http://www.w3.org/2000/svg", "rect");
            rect.setAttribute("class", "hit");
            rect.setAttribute("x", String(LABEL_BLOCK_ORIGIN));
            rect.setAttribute("y", "-12");
            rect.setAttribute("height", String(20 + d.data.attributes.length * 12));
            rect.addEventListener("click", () => {
                guardSelect(d, {});
            });
            this.insertBefore(rect, this.firstChild);
        });
    }

    /* --- camera --- */

    /** Run a reader-initiated camera change so the zoom handler pins it (not just notes it). */
    function userAction(change: () => void): void {
        userGesture = true;
        try {
            change();
        } finally {
            userGesture = false;
        }
    }

    /** Show a camera and fold; a `null` camera uses the default (root-centered) framing. */
    function applyView(camera: CameraTransform | null, fold: string[], zoom?: number | null): void {
        inheritedZoom = zoom ?? null;
        const token = ++viewToken;
        setFold(fold);
        update();
        if (camera) applyTransform(camera);
        else scheduleDefaultView(token);
    }

    /** Revert this graph to defaults: drop remembered state, expand all, re-frame. */
    function revert(): void {
        onRevert?.();
        applyView(null, []);
    }

    /**
     * Frame the stage once the container has a real size. A just-shown or hidden tab reads zero
     * until it lays out, so retry next frame (capped so a never-shown tab does not loop forever).
     * The `token` aborts the retry if a later applyView has superseded this one.
     *
     * A stage opens on the whole of what it draws, kept clear of the panels floating over the
     * canvas. Where the drawing cannot be measured — a DOM that lays nothing out — it falls back
     * to anchoring the root, which needs no measurement.
     */
    function scheduleDefaultView(token: number, attempt = 0): void {
        if (token !== viewToken) return;
        const parent = svg.node()?.parentElement ?? null;
        const width = parent?.clientWidth ?? 0;
        const height = parent?.clientHeight ?? 0;
        if (!parent || !width || !height) {
            if (attempt < 30) requestAnimationFrame(() => scheduleDefaultView(token, attempt + 1));
            return;
        }
        const insets = floatingPanelInsets(parent);
        // A reader who chose a zoom elsewhere keeps it: moving between stages should not silently
        // resize them. Only a stage arrived at with no zoom to inherit frames itself.
        const content = inheritedZoom === null ? drawnExtent() : null;
        if (content) {
            const fitted = frameToFit(content, { width, height }, insets, {
                minScale: LEGIBLE_ZOOM,
            });
            // Shrinking a long script until every node is on screen leaves an unreadable smudge.
            // Where the whole of it will not fit legibly, open at the start of it instead — the
            // reader can zoom out further by hand than a default should ever choose for them.
            if (fitted.k > LEGIBLE_ZOOM) {
                applyTransform(fitted);
                return;
            }
        }
        const rootX = (root as TreeNode).x ?? 0; // vertical position after layout
        const rootY = (root as TreeNode).y ?? 0; // horizontal position (0 at the root)
        const scale = content ? LEGIBLE_ZOOM : (inheritedZoom ?? DEFAULT_ZOOM);
        // Anchor the root inside the free rectangle, not the whole viewport: a stage that opens
        // at its start should no more begin underneath the legend than one that opens framed.
        const free = width - (insets.left ?? 0) - (insets.right ?? 0);
        const tx = (insets.left ?? 0) + free * ROOT_ANCHOR_X - scale * rootY;
        const ty = height / 2 - scale * rootX;
        applyTransform({ k: scale, x: tx, y: ty });
    }

    /** Everything the stage draws, in its own coordinates — bands, lines, and labels alike. */
    function drawnExtent(): Extent | null {
        const group = viewport.node();
        if (!group?.getBBox) return null;
        try {
            const box = group.getBBox();
            return box.width > 0 && box.height > 0 ? box : null;
        } catch {
            return null; // a DOM that lays nothing out has nothing to measure
        }
    }

    /**
     * The room the panels floating over the canvas ask to be kept clear of.
     *
     * The legend is the one that matters: it sits at the top right and has grown tall enough to
     * cover a good part of the drawing it describes.
     */
    function floatingPanelInsets(parent: Element): Insets {
        const canvas = parent.getBoundingClientRect?.();
        const panel = legend.getBoundingClientRect?.();
        if (!canvas || !panel || panel.width === 0) return {};
        return { right: Math.max(0, canvas.right - panel.left) + FLOATING_PANEL_GAP };
    }

    function centerOn(node: TreeNode): void {
        try {
            const size = viewportSize();
            const transform = zoomTransform(svg.node()!);
            const tx = size.width / 2 - node.y * transform.k;
            const ty = size.height / 2 - node.x * transform.k;
            svg.call(zoomBehavior.transform, zoomIdentity.translate(tx, ty).scale(transform.k));
        } catch {
            /* centring is optional */
        }
    }

    function clampScale(scale: number): number {
        return Math.max(0.1, Math.min(3, scale));
    }

    function applyTransform(transform: CameraTransform): void {
        try {
            svg.call(
                zoomBehavior.transform,
                zoomIdentity.translate(transform.x, transform.y).scale(transform.k),
            );
        } catch {
            /* leave the tree at its default position */
        }
    }

    /* --- fold --- */

    /** The ids of nodes that are collapsed (have hidden children) and currently visible. */
    function collapsedIds(): string[] {
        const ids: string[] = [];
        root.each((node) => {
            const treeNode = node as TreeNode;
            if (!treeNode.children && treeNode._children) ids.push(treeNode.data.id);
        });
        return ids;
    }

    /** Reset every node to expanded, then collapse exactly the ids that still exist. */
    function setFold(collapsed: readonly string[]): void {
        const wanted = new Set(collapsed);
        eachOriginal(root, (node) => {
            node.children = node._children;
            if (wanted.has(node.data.id)) node.children = undefined;
        });
    }

    /** Visit every node of the original (pre-collapse) hierarchy, top-down. */
    function eachOriginal(node: TreeNode, visit: (node: TreeNode) => void): void {
        visit(node);
        for (const child of node._children ?? []) eachOriginal(child, visit);
    }

    function viewportSize(): { width: number; height: number } {
        const parent = svg.node()?.parentElement;
        return {
            width: parent?.clientWidth || 800,
            height: parent?.clientHeight || 600,
        };
    }
}
