import type { Span, Stage } from "./model";
import { isFlow, type Neighbor } from "./neighbors";
import { tintsOf } from "./region-bands";

/**
 * A region seen as a thing in its own right: how much it holds, and how control gets in and out
 * of it.
 *
 * A scene is where a writer thinks in chapters — "who can reach this scene, and where does it let
 * you go?" — which the flow answers only by tracing every line at its border. This gathers that
 * border in one place.
 */
export interface RegionDetail {
    name: string;
    /** How many nodes the region holds. */
    nodeCount: number;
    /** Routes arriving from outside; each names the outside node it comes from. */
    entering: Neighbor[];
    /** Routes leaving for outside; each names the outside node it goes to. */
    leaving: Neighbor[];
    /**
     * The stretch of document the region covers, taken as the reach of its nodes' own spans, so a
     * client can show the text a scene was written as.
     */
    span?: Span;
    /** The tint its band is drawn with, so the region wears one color wherever it is named. */
    tint: number;
    /** What kind of grouping it is, as the compiler names it. */
    kind?: string;
    /** The slug a divert names it by. */
    anchor?: string;
    /**
     * Where the region is *declared* — a scene's heading. Distinct from {@link span}, which is
     * the ground its nodes cover: a reader jumping to a region wants the heading, not the body.
     */
    declaredAt?: Span;
}

export function regionDetailOf(stage: Stage, region: string): RegionDetail {
    const byId = new Map(stage.nodes.map((node) => [node.id, node]));
    const inside = stage.nodes.filter((node) => node.region === region);
    const entering: Neighbor[] = [];
    const leaving: Neighbor[] = [];

    for (const edge of stage.edges) {
        if (!isFlow(edge)) continue;
        const from = byId.get(edge.fromId);
        const to = byId.get(edge.toId);
        if (!from || !to || from.region === to.region) continue;
        if (to.region === region) {
            entering.push({
                id: from.id,
                ownerId: to.id,
                label: from.label,
                nodeCategory: from.category,
                edgeCategory: edge.category,
            });
        } else if (from.region === region) {
            leaving.push({
                id: to.id,
                ownerId: from.id,
                label: to.label,
                nodeCategory: to.category,
                edgeCategory: edge.category,
            });
        }
    }

    const declared = stage.regions?.find((each) => each.name === region);
    return {
        name: region,
        nodeCount: inside.length,
        kind: declared?.kind,
        anchor: declared?.anchor,
        declaredAt: declared?.span,
        entering,
        leaving,
        span: reachOf(inside),
        tint: tintsOf(stage.nodes.map((node) => node.region)).get(region) ?? 0,
    };
}

/** The stretch every one of these nodes fits inside, or undefined when none of them has a span. */
function reachOf(nodes: Stage["nodes"]): Span | undefined {
    const spans = nodes.map((node) => node.span).filter((span): span is Span => span !== undefined);
    if (spans.length === 0) return undefined;
    return {
        start: Math.min(...spans.map((span) => span.start)),
        end: Math.max(...spans.map((span) => span.end)),
    };
}
