import { describe, it, expect } from "vitest";
import { hierarchy } from "d3";
import { lineageIds, createTreeView } from "./tree-view";
import type { DisplayNode, Stage } from "./model";

interface Node {
    id: string;
    kids?: Node[];
}

/** A small tree: root → (a → a1, a2), b. */
function sample() {
    return hierarchy<Node>(
        {
            id: "root",
            kids: [{ id: "a", kids: [{ id: "a1" }, { id: "a2" }] }, { id: "b" }],
        },
        (datum) => datum.kids,
    );
}

const child = (node: ReturnType<typeof sample>, id: string) =>
    node.children!.find((candidate) => candidate.data.id === id)!;

describe("lineageIds", () => {
    it("includes the node, its ancestors, and its descendants", () => {
        const a = child(sample(), "a");
        expect(lineageIds(a)).toEqual(new Set(["root", "a", "a1", "a2"]));
    });

    it("for the root, spans the whole tree", () => {
        expect(lineageIds(sample())).toEqual(new Set(["root", "a", "a1", "a2", "b"]));
    });

    it("for a leaf, is just its path to the root", () => {
        const a1 = child(child(sample(), "a"), "a1");
        expect(lineageIds(a1)).toEqual(new Set(["root", "a", "a1"]));
    });

    it("respects collapse — a collapsed node's hidden descendants are excluded", () => {
        const a = child(sample(), "a");
        // The tree view collapses by moving children to _children, leaving children undefined.
        (a as { children?: unknown }).children = undefined;
        expect(lineageIds(a)).toEqual(new Set(["root", "a"]));
    });
});

/** A minimal stage: root → a, b, where `a` carries a source span that a recompile can shift. */
function stageWith(spanA: { start: number; end: number }): Stage {
    return {
        title: "AST",
        description: "",
        nodes: [
            { id: "root", label: "root", attributes: [] },
            { id: "a", label: "a", attributes: [], span: spanA },
            { id: "b", label: "b", attributes: [] },
        ],
        edges: [
            { fromId: "root", toId: "a", kind: "Child" },
            { fromId: "root", toId: "b", kind: "Child" },
        ],
    };
}

describe("createTreeView — selection by stable id", () => {
    it("selectById selects the resolved node and reports failure for an unknown id", () => {
        const selected: DisplayNode[] = [];
        const view = createTreeView(stageWith({ start: 0, end: 3 }), (n) => selected.push(n));

        expect(view.selectById("a")).toBe(true);
        expect(selected.at(-1)!.id).toBe("a");
        expect(view.selectById("ghost")).toBe(false); // cancels safely; no selection change
        expect(selected.at(-1)!.id).toBe("a");
    });

    it("resolves the id against the freshly installed view, not the stale spans", () => {
        // The click captured id "a" from the pre-save view (span 0..3). A save recompiled and
        // rebuilt the view with the node's span shifted (5..8). Resolving by id against the new
        // view selects the node with the CURRENT span, never the stale captured one.
        const staleView = createTreeView(stageWith({ start: 0, end: 3 }), () => {});
        void staleView;

        const freshSelected: DisplayNode[] = [];
        const freshView = createTreeView(stageWith({ start: 5, end: 8 }), (n) =>
            freshSelected.push(n),
        );

        expect(freshView.selectById("a")).toBe(true);
        expect(freshSelected.at(-1)!.span).toEqual({ start: 5, end: 8 });
    });

    it("selectById with toggle collapses the node's fold, like a circle click", () => {
        const view = createTreeView(stageWith({ start: 0, end: 3 }), () => {});
        expect(view.svg.querySelectorAll("g.node").length).toBe(3); // root, a, b

        view.selectById("root", { toggle: true }); // fold the root, hiding its subtree

        expect(view.svg.querySelectorAll("g.node").length).toBe(1); // only the collapsed root remains
    });

    it("leaves a closed fold closed when a selection is merely restored", () => {
        // A rebuild reselects the node that was showing. It must not reopen a branch the reader
        // deliberately shut — only deliberate navigation asks to be shown.
        const view = createTreeView(stageWith({ start: 0, end: 3 }), () => {});
        view.selectById("root", { toggle: true });

        expect(view.selectById("a")).toBe(true);

        expect(view.svg.querySelectorAll("g.node").length).toBe(1);
    });

    it("opens whatever is folded over a node reached by name, so selecting it shows it", () => {
        // A node reached from a search or a neighbor row may sit inside a collapsed branch.
        // Marking it there would fill the inspector while the drawing stayed shut.
        const view = createTreeView(stageWith({ start: 0, end: 3 }), () => {});
        view.selectById("root", { toggle: true });
        expect(view.svg.querySelectorAll("g.node").length).toBe(1);

        expect(view.selectById("a", { reveal: true })).toBe(true);

        expect(view.svg.querySelectorAll("g.node").length).toBe(3);
        expect(view.svg.querySelector("g.node.selected")).not.toBeNull();
    });

    // Fitting a long script leaves every node a few pixels tall. Centering there marks a node the
    // reader still cannot read, which is the same failure as leaving it folded away.
    it("brings a revealed node up to a readable scale", () => {
        const cameras: { k: number; byUser: boolean }[] = [];
        const view = createTreeView(stageWith({ start: 0, end: 3 }), () => {}, {
            initialCamera: { k: 0.15, x: 0, y: 0 },
            onCameraChange: (transform, byUser) => cameras.push({ k: transform.k, byUser }),
        });

        expect(view.selectById("a", { center: true, reveal: true })).toBe(true);

        expect(cameras.at(-1)?.k).toBeGreaterThanOrEqual(1);
        expect(cameras.at(-1)?.byUser).toBe(false);
    });

    it("keeps a reader who is already closer at their own scale", () => {
        const cameras: number[] = [];
        const view = createTreeView(stageWith({ start: 0, end: 3 }), () => {}, {
            initialCamera: { k: 2, x: 0, y: 0 },
            onCameraChange: (transform) => cameras.push(transform.k),
        });

        expect(view.selectById("a", { center: true, reveal: true })).toBe(true);

        expect(cameras.at(-1)).toBe(2);
    });

    it("leaves the scale alone when a selection is not a reveal", () => {
        const cameras: number[] = [];
        const view = createTreeView(stageWith({ start: 0, end: 3 }), () => {}, {
            initialCamera: { k: 0.15, x: 0, y: 0 },
            onCameraChange: (transform) => cameras.push(transform.k),
        });

        expect(view.selectById("a", { center: true })).toBe(true);

        expect(cameras.at(-1)).toBe(0.15);
    });
});

/**
 * A graph with two scenes and a line through both, like the Dialogue Graph tab draws:
 *
 * `root → market.1 → market.2 → forest.1 → tail`
 */
function scenedStage(): Stage {
    return {
        title: "Dialogue Graph",
        description: "",
        nodes: [
            { id: "root", label: "Document", attributes: [] },
            { id: "m1", label: "Fresh apples!", attributes: [], region: "The Market" },
            { id: "m2", label: "How much?", attributes: [], region: "The Market" },
            { id: "f1", label: "Branches close in", attributes: [], region: "The Forest" },
            { id: "tail", label: "End", attributes: [] },
        ],
        edges: [
            { fromId: "root", toId: "m1", kind: "Child", category: "structure" },
            { fromId: "m1", toId: "m2", kind: "Child", category: "succession" },
            { fromId: "m2", toId: "f1", kind: "Child", category: "jump" },
            { fromId: "f1", toId: "tail", kind: "Child", category: "succession" },
        ],
        regions: [
            { name: "The Market", kind: "Scene" },
            { name: "The Forest", kind: "Scene" },
        ],
    };
}

/** The band drawn for a named scene. */
function bandOf(view: { svg: SVGSVGElement }, region: string): SVGGElement {
    return [...view.svg.querySelectorAll<SVGGElement>("g.region")].find((band) =>
        band.querySelector("g.region-fold")?.getAttribute("aria-label")?.includes(region),
    )!;
}

/** Press a scene band's fold chevron, as a reader does. */
function foldScene(view: { svg: SVGSVGElement }, region: string): void {
    bandOf(view, region)
        .querySelector("g.region-fold")!
        .dispatchEvent(new MouseEvent("click", { bubbles: true }));
}

const drawnLabels = (view: { svg: SVGSVGElement }): string[] =>
    [...view.svg.querySelectorAll("g.node text.label")].map((text) => text.textContent ?? "");

describe("createTreeView — folding a scene", () => {
    it("offers a fold control on every scene band, open to begin with", () => {
        const view = createTreeView(scenedStage(), () => {});

        const controls = [...view.svg.querySelectorAll("g.region g.region-fold")];

        expect(controls).toHaveLength(2);
        expect(controls.map((control) => control.getAttribute("aria-expanded"))).toEqual([
            "true",
            "true",
        ]);
    });

    it("replaces the scene's nodes with one box named for it", () => {
        const view = createTreeView(scenedStage(), () => {});

        foldScene(view, "The Market");

        expect(drawnLabels(view)).toEqual(["Document", "The Market", "Branches close in", "End"]);
    });

    it("keeps the flow through the folded scene, so what follows still stands where it did", () => {
        const view = createTreeView(scenedStage(), () => {});

        foldScene(view, "The Market");

        // root → box → forest → tail: one line fewer, and nothing downstream lost.
        expect(view.svg.querySelectorAll("path.link").length).toBe(3);
        expect(drawnLabels(view)).toContain("End");
    });

    it("says the scene is shut, on the band and on its control", () => {
        const view = createTreeView(scenedStage(), () => {});

        foldScene(view, "The Market");

        const band = bandOf(view, "The Market");
        expect(band.classList.contains("folded")).toBe(true);
        expect(band.querySelector("g.region-fold")!.getAttribute("aria-expanded")).toBe("false");
        // The box carries the name now, so the band does not write it twice.
        expect(band.querySelector("text.region-name")!.textContent).toBe("");
    });

    it("opens the scene again when the control is pressed a second time", () => {
        const view = createTreeView(scenedStage(), () => {});

        foldScene(view, "The Market");
        foldScene(view, "The Market");

        expect(drawnLabels(view)).toEqual([
            "Document",
            "Fresh apples!",
            "How much?",
            "Branches close in",
            "End",
        ]);
    });

    it("folds each scene independently", () => {
        const view = createTreeView(scenedStage(), () => {});

        foldScene(view, "The Market");
        foldScene(view, "The Forest");

        expect(drawnLabels(view)).toEqual(["Document", "The Market", "The Forest", "End"]);
    });

    it("tells the caller which scenes are folded, so a tab switch can restore them", () => {
        const folds: string[][] = [];
        const view = createTreeView(scenedStage(), () => {}, {
            onRegionFoldChange: (collapsed) => folds.push(collapsed),
        });

        foldScene(view, "The Market");

        expect(folds).toEqual([["The Market"]]);
    });

    it("opens with the scenes it was told were folded", () => {
        const view = createTreeView(scenedStage(), () => {}, {
            initialRegionFold: ["The Forest"],
        });

        expect(drawnLabels(view)).toEqual([
            "Document",
            "Fresh apples!",
            "How much?",
            "The Forest",
            "End",
        ]);
    });

    it("leaves the reader's chosen node alone when the fold did not hide it", () => {
        const chosen: DisplayNode[] = [];
        const view = createTreeView(scenedStage(), (node) => chosen.push(node));
        view.selectById("tail");

        foldScene(view, "The Market");

        expect(chosen.at(-1)!.id).toBe("tail");
        expect(view.svg.querySelector("g.node.selected")).not.toBeNull();
    });

    it("moves the selection to the scene that swallowed the chosen node", () => {
        const regions: string[] = [];
        const view = createTreeView(scenedStage(), () => {}, {
            onSelectRegion: (region) => regions.push(region),
        });
        view.selectById("m2");

        foldScene(view, "The Market");

        expect(regions).toEqual(["The Market"]);
        expect(bandOf(view, "The Market").classList.contains("selected")).toBe(true);
    });

    it("shows the scene, not a node, when its box is clicked", () => {
        const regions: string[] = [];
        const view = createTreeView(scenedStage(), () => {}, {
            onSelectRegion: (region) => regions.push(region),
        });
        foldScene(view, "The Market");

        view.svg
            .querySelector("g.node.region-box circle")!
            .dispatchEvent(new MouseEvent("click", { bubbles: true }));

        expect(regions.at(-1)).toBe("The Market");
    });

    it("opens a folded scene to show a node deliberately navigated to", () => {
        const view = createTreeView(scenedStage(), () => {}, { initialRegionFold: ["The Market"] });
        expect(drawnLabels(view)).not.toContain("How much?");

        expect(view.selectById("m2", { reveal: true })).toBe(true);

        expect(drawnLabels(view)).toContain("How much?");
        expect(view.svg.querySelector("g.node.selected")).not.toBeNull();
    });

    it("leaves a folded scene shut when a selection is merely restored", () => {
        const view = createTreeView(scenedStage(), () => {}, { initialRegionFold: ["The Market"] });

        expect(view.selectById("m2")).toBe(false);

        expect(drawnLabels(view)).not.toContain("How much?");
    });

    it("finds the route into a folded scene by the nodes the document named", () => {
        const view = createTreeView(scenedStage(), () => {}, { initialRegionFold: ["The Market"] });

        expect(view.selectEdgeBetween("root", "m1")).toBe(true);
    });

    it("restores the folds an applyView asks for", () => {
        const view = createTreeView(scenedStage(), () => {});

        view.applyView(null, [], null, ["The Forest"]);

        expect(drawnLabels(view)).toContain("The Forest");
        expect(drawnLabels(view)).not.toContain("Branches close in");
    });
});

describe("createTreeView — the fold control keeps to its own corner", () => {
    it("dresses the band and the control in classes of their own", () => {
        // The stylesheet paints a folded band with a broken edge. A rule reaching every rect
        // inside the band would dress the control's invisible target in it too, drawing a box
        // around the chevron that reads as a stray outline.
        const view = createTreeView(scenedStage(), () => {}, { initialRegionFold: ["The Market"] });
        const band = bandOf(view, "The Market");

        expect(band.querySelector("rect.region-band")).not.toBeNull();
        expect(band.querySelector("g.region-fold rect.region-fold-hit")).not.toBeNull();
        expect(band.querySelectorAll("rect.region-band")).toHaveLength(1);
    });

    it("keeps the control clear of the node beneath it, even folded to one box", () => {
        // A folded band closes to a single node's width, so a control on the node's row would
        // sit on top of its dot and its count.
        const view = createTreeView(scenedStage(), () => {}, { initialRegionFold: ["The Market"] });

        expect(controlBottomOf(view, "The Market")).toBeLessThan(boxTopOf(view));
    });
});

/** How far down the fold control's pointer target reaches, in drawing coordinates. */
function controlBottomOf(view: { svg: SVGSVGElement }, region: string): number {
    const control = bandOf(view, region).querySelector("g.region-fold")!;
    const [, y] = /translate\(([-\d.]+),([-\d.]+)\)/
        .exec(control.getAttribute("transform")!)!
        .slice(1);
    const hit = control.querySelector("rect.region-fold-hit")!;
    return Number(y) + Number(hit.getAttribute("y")) + Number(hit.getAttribute("height"));
}

/** The top of the folded box's own dot, in the same coordinates. */
function boxTopOf(view: { svg: SVGSVGElement }): number {
    const box = view.svg.querySelector("g.node.region-box")!;
    const [, y] = /translate\(([-\d.]+),([-\d.]+)\)/.exec(box.getAttribute("transform")!)!.slice(1);
    return Number(y) - Number(box.querySelector("circle")!.getAttribute("r"));
}

describe("createTreeView — folding a scene from the keyboard", () => {
    const press = (key: string) => new KeyboardEvent("keydown", { key });

    /** Put the pointer on a drawn node, as moving over it does. */
    function hover(view: { svg: SVGSVGElement }, label: string): void {
        [...view.svg.querySelectorAll("g.node")]
            .find((node) => node.textContent?.includes(label))!
            .dispatchEvent(new MouseEvent("mouseenter", { bubbles: true }));
    }

    it("opens the folded box the pointer rests on", () => {
        // Reaching a shut scene and pressing Enter should open it — that is what the box is for.
        const view = createTreeView(scenedStage(), () => {}, { initialRegionFold: ["The Market"] });
        hover(view, "The Market");

        view.handleKey(press("Enter"));

        expect(drawnLabels(view)).toContain("Fresh apples!");
    });

    it("opens the folded box on Space too, not only Enter", () => {
        const view = createTreeView(scenedStage(), () => {}, { initialRegionFold: ["The Market"] });
        hover(view, "The Market");

        view.handleKey(press(" "));

        expect(drawnLabels(view)).toContain("Fresh apples!");
    });

    it("does not shut a scene under a reader resting on one of its lines", () => {
        // The fold key acts on a scene the reader is *on as a thing* — its box, or the scene they
        // chose. Hovering a line and pressing Space must not take the whole scene away with it.
        const view = createTreeView(scenedStage(), () => {});
        hover(view, "Fresh apples!");

        view.handleKey(press(" "));

        expect(drawnLabels(view)).toContain("Fresh apples!");
    });

    it("folds the scene the reader chose, when the pointer is over nothing", () => {
        const view = createTreeView(scenedStage(), () => {});
        view.selectRegion("The Market");

        view.handleKey(press("Enter"));

        expect(drawnLabels(view)).toContain("The Market");
        expect(drawnLabels(view)).not.toContain("Fresh apples!");
    });

    it("stops fading the drawing once the node the pointer was on has gone", () => {
        // The spotlight follows the pointer's node. Folded away, a stale one would dim every
        // other node while lighting none of them.
        const view = createTreeView(scenedStage(), () => {});
        hover(view, "Fresh apples!");
        expect(view.svg.classList.contains("has-focus")).toBe(true);

        foldScene(view, "The Market");

        expect(view.svg.classList.contains("has-focus")).toBe(false);
    });

    it("leaves a stage with no scenes to its own keys", () => {
        const view = createTreeView(stageWith({ start: 0, end: 3 }), () => {});
        view.selectById("root");

        view.handleKey(press("Enter"));

        expect(view.svg.querySelectorAll("g.node")).toHaveLength(1); // the root, folded as before
    });
});
