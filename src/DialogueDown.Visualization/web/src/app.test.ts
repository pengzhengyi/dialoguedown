import { describe, it, expect, beforeEach, vi } from "vitest";
import { runApp } from "./app";
import type { Report, Stage } from "./model";
import { mermaidPreviews } from "./mermaid-preview";

/**
 * The report skeleton `runApp` binds to — the ids it and its helpers query. The nodes below are
 * deliberately sourceless: the inspector shows a note (not a CodeMirror editor) so the test
 * exercises the selection-preservation logic without mounting the editor, whose jsdom layout
 * measurement is covered end-to-end by Playwright instead.
 */
function mountDom(): void {
    document.body.innerHTML = `
        <nav id="tabs"></nav>
        <div id="live-banner" hidden></div>
        <main id="app">
            <section id="stages"></section>
            <div id="resizer"></div>
            <aside id="detail">
                <header id="detail-title">Node details</header>
                <div id="detail-body"></div>
            </aside>
        </main>
        <footer>
            <span id="help-summary"></span>
            <button id="help-toggle" aria-expanded="false" aria-controls="help-content"></button>
            <div id="help-content" hidden></div>
            <div id="footer-drawer"></div>
        </footer>
        <div class="status-bar"></div>
    `;
}

/**
 * The single stage `runApp` mounts: a fork and a join. `root` leads to `a` and `b`, and both lead
 * to `c`. `b` is listed before `a` so the layout's first child differs from the first outgoing
 * edge (they follow node and edge order respectively), which lets a test tell which one a key
 * follows. `a`'s label is overridable and `a` itself can be dropped, so a rebuild can lose the
 * selected node.
 */
function stage(options: { labelOfA?: string; dropA?: boolean } = {}): Stage {
    const nodes = [
        { id: "root", label: "root", attributes: [] },
        { id: "b", label: "beta", attributes: [] },
        { id: "a", label: options.labelOfA ?? "alpha", attributes: [] },
        { id: "c", label: "gamma", attributes: [] },
    ].filter((node) => !(options.dropA && node.id === "a"));
    const edges = [
        { fromId: "root", toId: "a", kind: "Child" as const },
        { fromId: "root", toId: "b", kind: "Child" as const },
        { fromId: "a", toId: "c", kind: "Child" as const },
        { fromId: "b", toId: "c", kind: "Child" as const },
    ].filter((edge) => !(options.dropA && (edge.toId === "a" || edge.fromId === "a")));
    return { title: "AST", description: "", nodes, edges, nests: true };
}

/**
 * The same fork and join as the flow graph: its child edges span the flow rather than nesting, and
 * every edge names its route, so the stage is the one whose ways in answer to Shift+digit. Kept
 * apart from `stage()` — which a nesting tree is — because the keyboard rule follows the stage's
 * declared shape, not its title or its palette.
 */
function graphStage(): Stage {
    const edges = stage().edges.map((edge) => ({ ...edge, category: "choice" as const }));
    return { ...stage(), title: "Dialogue Graph", edges, nests: false };
}

function reportWith(labelOfA: string): Report {
    return { source: "root\nalpha\nbeta", stages: [stage({ labelOfA })] };
}

function graphReport(): Report {
    return { source: "root\nalpha\nbeta", stages: [graphStage()] };
}

const arrow = (key: string) =>
    document.body.dispatchEvent(new KeyboardEvent("keydown", { key, bubbles: true }));

const detailTitle = () => document.getElementById("detail-title")!.textContent ?? "";
const selectedCount = () =>
    document.querySelectorAll("section.stage.active g.node.selected").length;

/** A direct click on a drawn node's circle, as a reader makes with a mouse. */
function clickNode(label: string): void {
    const node = [...document.querySelectorAll<SVGGElement>("section.stage.active g.node")].find(
        (group) => group.querySelector("text.label")?.textContent === label,
    );
    node!.querySelector("circle")!.dispatchEvent(new MouseEvent("click", { bubbles: true }));
}

/** Open the AST graph tab — the report opens on Source. */
function openAstTab(): void {
    document.querySelectorAll<HTMLButtonElement>("#tabs .tab")[1].click();
}

/** Open the AST graph tab, then select node `a` by following root's first way out. */
function selectNodeA(): void {
    openAstTab();
    arrow("ArrowDown"); // selects the root immediately
    arrow("ArrowRight"); // follows root's first outgoing edge to `a`
}

describe("runApp updateStages — inspector selection across a rebuild", () => {
    beforeEach(() => {
        mountDom();
    });

    it("keeps the selection on the same node id and rebinds it to the recompiled node", () => {
        const app = runApp(reportWith("alpha"), { editable: true, onChange: () => {} });
        selectNodeA();
        expect(detailTitle()).toContain("alpha");
        expect(selectedCount()).toBe(1);

        // A successful idle autosave recompiles and rebuilds the graph tabs. The node keeps its id
        // but its label changes; the inspector must stay open, resolved against the fresh view.
        app.updateStages([stage({ labelOfA: "alpha (recompiled)" })]);

        expect(detailTitle()).toContain("alpha (recompiled)");
        expect(selectedCount()).toBe(1);
    });

    it("clears the inspector safely when the selected node is gone after a rebuild", () => {
        const app = runApp(reportWith("alpha"), { editable: true, onChange: () => {} });
        selectNodeA();
        expect(detailTitle()).toContain("alpha");

        app.updateStages([stage({ dropA: true })]);

        expect(detailTitle()).toBe("Node details");
        expect(selectedCount()).toBe(0);
    });

    it("does not throw when nothing is selected (Source tab active) during a rebuild", () => {
        const app = runApp(reportWith("alpha"), { editable: true, onChange: () => {} });

        expect(() => app.updateStages([stage()])).not.toThrow();
        expect(detailTitle()).toBe("Node details");
    });

    it("disposes a Semantic preview host before replacing its stage", () => {
        const dispose = vi.spyOn(mermaidPreviews, "dispose");
        const semantic = { ...stage(), title: "Semantic Model", tables: [] };
        const app = runApp(
            { source: "root\nalpha\nbeta", stages: [semantic] },
            { editable: true, onChange: () => {} },
        );
        const oldBody = document.querySelector<HTMLElement>(".semantic-stage .node-detail-body")!;
        dispose.mockClear();

        app.updateStages([{ ...semantic, nodes: [...semantic.nodes] }]);

        expect(dispose).toHaveBeenCalledWith(oldBody);
        dispose.mockRestore();
    });
});

describe("runApp showConfigTab", () => {
    it("activates the Config tab (the Explorer's open-config)", () => {
        mountDom();
        const app = runApp({ source: "root", stages: [stage()], configuration: { speakers: [] } });
        const tabs = () => document.querySelectorAll<HTMLElement>("#tabs .tab");
        // The Config tab is first (index 0); the report opens on Source, so Config starts inactive.
        expect(tabs()[0].classList.contains("active")).toBe(false);

        app.showConfigTab();

        expect(tabs()[0].classList.contains("active")).toBe(true);
    });
});

describe("runApp Playbook tab", () => {
    const playbook = () => ({
        json: '{ "entry": 0 }',
        metadata: {
            script: "s.dialogue.md",
            formatVersion: 0,
            schemaUrl: "https://pengzhengyi.github.io/dialoguedown/schema/playbook-0.schema.json",
            requires: ["core"],
            uses: [],
            entry: 0,
            nodeCount: 2,
            anchorCount: 0,
        },
        anchors: [{ name: "the-tavern", node: 0 }],
        speakers: [{ name: "Alice", default: false, tags: [] }],
        nodes: [],
    });
    const titles = () => [...document.querySelectorAll("#tabs .tab")].map((t) => t.textContent);

    beforeEach(mountDom);

    it("comes last, after the graph the playbook is compiled from", () => {
        const graph = graphStage();

        runApp({ source: "root", stages: [stage(), graph], playbook: playbook() });

        expect(titles()).toEqual(["Source", "AST", "Dialogue Graph", "Playbook"]);
    });

    it("is absent from a report built without the compiler", () => {
        runApp({ source: "root", stages: [stage()] });

        expect(titles()).not.toContain("Playbook");
    });

    it("shows the recompiled playbook after a save replaces the stages", () => {
        const app = runApp({ source: "root", stages: [stage()], playbook: playbook() });

        app.updateStages([stage()], {
            ...playbook(),
            anchors: [{ name: "the-tavern", node: 0 }],
            speakers: [{ name: "Bob", default: false, tags: [] }],
        });

        expect(titles()).toEqual(["Source", "AST", "Playbook"]);
        expect(document.querySelector(".playbook-side")?.textContent).toContain("Bob");
    });

    it("keeps the last playbook when a recompile does not carry one", () => {
        const app = runApp({ source: "root", stages: [stage()], playbook: playbook() });

        app.updateStages([stage()]);

        expect(titles()).toContain("Playbook");
        expect(document.querySelector(".playbook-side")?.textContent).toContain("Alice");
    });
});

describe("runApp Playbook help", () => {
    it("explains the playbook rather than the Source editor", () => {
        mountDom();
        const graph = graphStage();
        const app = runApp({
            source: "root",
            stages: [graph],
            playbook: {
                json: "{}",
                anchors: [{ name: "the-tavern", node: 0 }],
                speakers: [],
                nodes: [],
            },
        });
        app.updateStages([graph]);

        const tabs = [...document.querySelectorAll<HTMLElement>("#tabs .tab")];
        tabs.at(-1)!.click();

        expect(document.getElementById("help-content")?.innerHTML).toContain("read-only");
        expect(document.getElementById("help-content")?.innerHTML).toContain("playbook");
    });
});

describe("runApp — what the footer offers", () => {
    beforeEach(() => {
        mountDom();
    });

    const drawerTabs = () => [...document.querySelectorAll("#footer-drawer .drawer-tab")];

    // The file selector is a report with no source: there is nothing to diagnose and no editor to
    // jump into, so the Problems panel, its drawer tab, and its counts stay out of the way. Help
    // stays — it describes the Explorer, which is the one thing to do here.
    it("leaves the Problems panel out of a report with no script", () => {
        runApp({ stages: [] });

        expect(drawerTabs().map((tab) => tab.textContent)).not.toContain("Problems");
        expect(document.querySelector(".status-bar .diagnostic-summary")).toBeNull();
    });

    it("offers the Problems panel once the report has a script", () => {
        runApp(reportWith("alpha"));

        expect(drawerTabs().map((tab) => tab.textContent)).toContain("Problems");
        expect(document.querySelector(".status-bar .diagnostic-summary")).not.toBeNull();
    });
});

describe("runApp — keyboard navigation follows the graph's edges", () => {
    beforeEach(() => {
        mountDom();
        runApp(reportWith("alpha"));
        openAstTab();
    });

    const press = (init: KeyboardEventInit) =>
        document.body.dispatchEvent(new KeyboardEvent("keydown", { bubbles: true, ...init }));

    /** A top-row digit as the browser reports it: the code carries the number. */
    const digit = (value: number) => press({ key: String(value), code: `Digit${value}` });

    it("→ follows the first outgoing edge in document order, not the layout's first child", () => {
        arrow("ArrowDown"); // nothing selected yet: the root

        arrow("ArrowRight");

        // The fixture lists `b` before `a`, so a walk of the drawing would land on beta.
        expect(detailTitle()).toContain("alpha");
    });

    it("moves to the previous and next sibling in the drawing, wrapping at the ends", () => {
        arrow("ArrowDown"); // nothing selected yet: the root
        arrow("ArrowRight"); // the first way out, alpha

        arrow("ArrowDown");
        expect(detailTitle()).toContain("beta");
        arrow("ArrowDown"); // wrapped past the last sibling back to alpha
        expect(detailTitle()).toContain("alpha");
        arrow("ArrowUp"); // and back from the first
        expect(detailTitle()).toContain("beta");
    });

    it("leaves a node alone when the drawing gives it no siblings", () => {
        clickNode("gamma"); // beta's only drawn child

        arrow("ArrowUp");
        expect(detailTitle()).toContain("gamma");
        arrow("ArrowDown");
        expect(detailTitle()).toContain("gamma");
    });

    it("walks back along the edges the keyboard took", () => {
        arrow("ArrowDown");
        arrow("ArrowRight"); // root → alpha
        arrow("ArrowRight"); // alpha → gamma
        expect(detailTitle()).toContain("gamma");

        arrow("ArrowLeft");
        expect(detailTitle()).toContain("alpha"); // the edge just taken, walked back
        arrow("ArrowLeft");
        expect(detailTitle()).toContain("root"); // and the edge before that one
    });

    it("walks back to the node a digit jumped from", () => {
        arrow("ArrowDown");
        digit(2); // root's second way out: beta
        expect(detailTitle()).toContain("beta");

        arrow("ArrowLeft");

        expect(detailTitle()).toContain("root");
    });

    it("goes to the first way in when the trail is empty", () => {
        clickNode("gamma"); // a direct selection, so there is no trail

        arrow("ArrowLeft");

        // Both alpha and beta lead to gamma; the stage lists alpha first.
        expect(detailTitle()).toContain("alpha");
    });

    it("does not throw when ← has neither a trail nor a way in", () => {
        arrow("ArrowDown"); // the root, chosen by the arrow itself
        expect(() => arrow("ArrowLeft")).not.toThrow();

        // Nothing leads to the root, so there is nowhere to walk back to.
        expect(detailTitle()).toContain("root");
    });

    it("a click forgets the trail, so ← starts over from the first way in", () => {
        arrow("ArrowDown");
        arrow("ArrowRight"); // root → alpha, leaving root on the trail
        clickNode("gamma");
        expect(detailTitle()).toContain("gamma");

        arrow("ArrowLeft");

        expect(detailTitle()).toContain("alpha"); // not root, the abandoned trail
    });

    it("takes the nth way out with a digit, read from the key's code", () => {
        arrow("ArrowDown");

        digit(2);

        expect(detailTitle()).toContain("beta");
    });

    it("leaves Shift+digit to the browser on a tree stage, even where two edges lead in", () => {
        clickNode("gamma"); // a and b both lead here, but a tree's ways in are not addressed

        // Shift makes the browser report `@` for `2`; the key is not the tree's, so it is not
        // consumed and the selection stays where it was.
        const event = new KeyboardEvent("keydown", {
            key: "@",
            code: "Digit2",
            shiftKey: true,
            bubbles: true,
            cancelable: true,
        });
        document.body.dispatchEvent(event);

        expect(event.defaultPrevented).toBe(false);
        expect(detailTitle()).toContain("gamma");
    });

    it("does not count the numpad's digits, which carry their own codes", () => {
        // Numpad2 reports `2` as its key but does not answer the drawing's numbering: the
        // inspector's digits are promised to the number row.
        arrow("ArrowDown"); // the root
        press({ key: "2", code: "Numpad2" });

        expect(detailTitle()).toContain("root");
    });

    it("leaves Ctrl and Cmd with the browser", () => {
        arrow("ArrowDown"); // the root

        press({ key: "2", code: "Digit2", ctrlKey: true });
        press({ key: "2", code: "Digit2", metaKey: true });

        expect(detailTitle()).toContain("root");
    });

    it("leaves a digit beyond the list where the selection is", () => {
        arrow("ArrowDown"); // the root, with two ways out

        digit(9);

        expect(detailTitle()).toContain("root");
    });

    it("does nothing with a digit until a node is chosen", () => {
        digit(1);

        expect(detailTitle()).toBe("Node details");
        expect(selectedCount()).toBe(0);
    });

    it("leaves folding to Enter and Space: → does not open a folded node", () => {
        clickNode("root"); // the circle also folds the root away
        expect(document.querySelectorAll("section.stage.active g.node")).toHaveLength(1);

        arrow("ArrowRight");

        expect(document.querySelectorAll("section.stage.active g.node")).toHaveLength(1);
    });
});

describe("runApp — the Dialogue Graph's ways in answer Shift", () => {
    beforeEach(() => {
        mountDom();
        runApp(graphReport());
        openAstTab();
    });

    const press = (init: KeyboardEventInit) =>
        document.body.dispatchEvent(new KeyboardEvent("keydown", { bubbles: true, ...init }));

    it("takes the nth way in with Shift and the same digit", () => {
        clickNode("gamma");

        // Shift makes the browser report `@` for `2`, so only the code still names the digit.
        press({ key: "@", code: "Digit2", shiftKey: true });

        expect(detailTitle()).toContain("beta");
    });

    it("still counts the nth way out with a plain digit", () => {
        arrow("ArrowDown"); // the root

        press({ key: "2", code: "Digit2" });

        expect(detailTitle()).toContain("beta");
    });
});

describe("runApp — the help follows the stage's shape", () => {
    it("gives a tree stage the tree keymap and the Dialogue Graph the full one", () => {
        mountDom();
        runApp({ source: "root", stages: [stage(), graphStage()] });
        const tabs = document.querySelectorAll<HTMLButtonElement>("#tabs .tab");
        const help = () => document.getElementById("help-content")!.innerHTML;

        tabs[1].click(); // Markdown AST — a tree
        expect(help()).toContain("first child");
        expect(help()).not.toContain("Shift");

        tabs[2].click(); // Dialogue Graph — the flow
        expect(help()).toContain("Shift");
    });
});

describe("runApp — keys stay with the tab that owns them", () => {
    it("leaves the graph alone while another tab is active", () => {
        mountDom();
        runApp(reportWith("alpha"));
        openAstTab();
        arrow("ArrowDown"); // the root, so the graph has a selection to keep
        expect(detailTitle()).toContain("root");

        document.querySelectorAll<HTMLButtonElement>("#tabs .tab")[0].click(); // the Source tab
        arrow("ArrowDown");
        arrow("ArrowRight");
        arrow("2");

        // Switching cleared the graph's selection and the inspector; a key that leaked into the
        // graph would have selected its root and reopened the inspector.
        expect(detailTitle()).toBe("Node details");
    });

    it("never takes a character away from a text field", () => {
        mountDom();
        runApp(reportWith("alpha"));
        openAstTab();
        const input = document.createElement("input");
        document.body.appendChild(input);
        const event = new KeyboardEvent("keydown", {
            key: "2",
            code: "Digit2",
            bubbles: true,
        });

        input.dispatchEvent(event);

        // The graph would otherwise take the digit for its second way out; the field keeps it.
        expect(event.defaultPrevented).toBe(false);
        expect(detailTitle()).toBe("Node details");
    });
});
