/** A graph's zoom scale and pan translation — the D3 zoom transform. */
export interface CameraTransform {
    k: number;
    x: number;
    y: number;
}

/**
 * Remembers graph positions with a hybrid policy:
 *
 * - a shared **current** *zoom* that untouched graphs inherit, so moving between
 *   graphs keeps them at one scale;
 * - a per-graph **override** that a graph pins the moment the reader adjusts it, so
 *   an adjusted graph keeps its own camera regardless of the shared one;
 * - per-graph **fold** state (which nodes are collapsed), always independent
 *   because nodes differ between graphs.
 *
 * Purely in-memory for the life of the page — nothing is serialized or sent to the
 * server, so the offline single-file report stays self-contained.
 */
export class GraphCameraStore {
    private readonly overrides = new Map<string, CameraTransform>();
    private readonly folds = new Map<string, string[]>();
    private readonly regionFolds = new Map<string, string[]>();
    private current: CameraTransform | null = null;

    /**
     * The camera a graph should show: its own pinned override, else `null` (meaning "frame it
     * from its own root").
     *
     * Only the *zoom* is shared — see {@link inheritedZoom}. A pan means something only against
     * the graph it was made on, and graphs differ wildly in extent: the dialogue graph runs far
     * wider than the trees beside it, so carrying a pan into it scrolls its nodes off-screen and
     * leaves the reader looking at nothing.
     */
    cameraFor(title: string): CameraTransform | null {
        return this.overrides.get(title) ?? null;
    }

    /** The zoom an untouched graph opens at, so moving between graphs keeps one scale. */
    inheritedZoom(title: string): number | null {
        return this.overrides.has(title) ? null : (this.current?.k ?? null);
    }

    /** The collapsed node ids remembered for a graph (empty when untouched). */
    foldFor(title: string): string[] {
        return this.folds.get(title) ?? [];
    }

    /** The folded scene names remembered for a graph (empty when untouched). */
    regionFoldFor(title: string): string[] {
        return this.regionFolds.get(title) ?? [];
    }

    /**
     * Record a reader-driven camera change: pin the graph's override and make it the
     * shared current camera that other untouched graphs inherit.
     */
    adjustCamera(title: string, transform: CameraTransform): void {
        this.overrides.set(title, transform);
        this.current = transform;
    }

    /**
     * Track the live camera — including programmatic applies (a reveal, the default
     * framing) — so the shared current camera reflects wherever the reader is now,
     * without pinning an override.
     */
    noteCamera(transform: CameraTransform): void {
        this.current = transform;
    }

    /** Remember a reader-driven fold change for a graph. */
    setFold(title: string, collapsed: string[]): void {
        this.folds.set(title, collapsed);
    }

    /** Remember which scenes a reader has folded away in a graph. */
    setRegionFold(title: string, collapsed: string[]): void {
        this.regionFolds.set(title, collapsed);
    }

    /** Revert a graph to defaults: drop its camera override and its fold memory. */
    reset(title: string): void {
        this.overrides.delete(title);
        this.folds.delete(title);
        this.regionFolds.delete(title);
    }
}
