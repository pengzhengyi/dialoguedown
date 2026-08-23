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
        </footer>
    `;
}

/** The single graph stage root -> a, b, with `a`'s label overridable and optionally removed. */
function stage(options: { labelOfA?: string; dropA?: boolean } = {}): Stage {
    const nodes = [
        { id: "root", label: "root", attributes: [] },
        { id: "a", label: options.labelOfA ?? "alpha", attributes: [] },
        { id: "b", label: "beta", attributes: [] },
    ].filter((node) => !(options.dropA && node.id === "a"));
    const edges = [
        { fromId: "root", toId: "a", kind: "Child" as const },
        { fromId: "root", toId: "b", kind: "Child" as const },
    ].filter((edge) => !(options.dropA && edge.toId === "a"));
    return { title: "AST", description: "", nodes, edges };
}

function reportWith(labelOfA: string): Report {
    return { source: "root\nalpha\nbeta", stages: [stage({ labelOfA })] };
}

const arrow = (key: string) =>
    document.body.dispatchEvent(new KeyboardEvent("keydown", { key, bubbles: true }));

const detailTitle = () => document.getElementById("detail-title")!.textContent ?? "";
const selectedCount = () =>
    document.querySelectorAll("section.stage.active g.node.selected").length;

/** Open the AST graph tab (the report opens on Source), then select node `a` in it. */
function selectNodeA(): void {
    document.querySelectorAll<HTMLButtonElement>("#tabs .tab")[1].click(); // AST tab
    arrow("ArrowDown"); // selects the root immediately
    arrow("ArrowRight"); // moves to child `a` — immediate without a navigation boundary
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
    });
    const titles = () => [...document.querySelectorAll("#tabs .tab")].map((t) => t.textContent);

    beforeEach(mountDom);

    it("comes last, after the graph the playbook is compiled from", () => {
        const graph = { ...stage(), title: "Dialogue Graph" };

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
        const graph = { ...stage(), title: "Dialogue Graph" };
        const app = runApp({
            source: "root",
            stages: [graph],
            playbook: {
                json: "{}",
                anchors: [{ name: "the-tavern", node: 0 }],
                speakers: [],
            },
        });
        app.updateStages([graph]);

        const tabs = [...document.querySelectorAll<HTMLElement>("#tabs .tab")];
        tabs.at(-1)!.click();

        expect(document.getElementById("help-content")?.innerHTML).toContain("read-only");
        expect(document.getElementById("help-content")?.innerHTML).toContain("playbook");
    });
});
