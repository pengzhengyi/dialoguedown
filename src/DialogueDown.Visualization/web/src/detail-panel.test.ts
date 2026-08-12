import { describe, it, expect, beforeEach } from "vitest";
import { createDetailPanel, type DetailPanel } from "./detail-panel";
import { colorOf } from "./palette";
import type { DisplayNode } from "./model";

describe("createDetailPanel", () => {
    let panel: DetailPanel;
    let title: HTMLElement;
    let body: HTMLElement;

    beforeEach(() => {
        document.body.innerHTML = `<h2 id="detail-title"></h2><div id="detail-body"></div>`;
        title = document.getElementById("detail-title")!;
        body = document.getElementById("detail-body")!;
        panel = createDetailPanel();
    });

    it("starts with a placeholder that points at the Source tab", () => {
        expect(body.textContent).toContain("Click any node");
        expect(body.textContent).toContain("Jump to source");
    });

    it("shows a category color dot and the escaped label", () => {
        const node: DisplayNode = {
            id: "n1",
            label: "Code <span>",
            category: "call",
            attributes: [],
        };
        panel.show(node);
        const dot = title.querySelector<HTMLElement>(".dot");
        expect(dot?.style.background).toBeTruthy();
        expect(title.textContent).toContain("Code <span>");
        expect(title.innerHTML).toContain("&lt;span&gt;");
        const probe = document.createElement("span");
        probe.style.background = colorOf("call");
        expect(dot?.style.background).toBe(probe.style.background);
    });

    it("omits the dot when the node has no category", () => {
        panel.show({ id: "n1", label: "Text", attributes: [] });
        expect(title.querySelector(".dot")).toBeNull();
    });

    it("renders a table of attributes", () => {
        panel.show({
            id: "n1",
            label: "Heading",
            attributes: [
                { name: "level", value: "2" },
                { name: "text", value: "Scene" },
            ],
        });
        const rows = body.querySelectorAll("table tr");
        expect(rows).toHaveLength(2);
        expect(rows[0].querySelector("th")?.textContent).toBe("level");
        expect(rows[0].querySelector("td")?.textContent).toBe("2");
    });

    it("shows the node's source and a rendered preview, read-only", () => {
        panel.show({
            id: "n1",
            label: "Heading",
            attributes: [],
            source: "# Scene",
            span: { start: 0, end: 7 },
        });

        expect(body.querySelector("pre code")?.textContent).toBe("# Scene");
        expect(body.querySelector(".preview")?.innerHTML).toContain("<h1>Scene</h1>");
        // Editing lives in the Source tab; the inspector never mounts an editor.
        expect(body.querySelector(".cm-editor")).toBeNull();
    });

    it("escapes source so a node's text cannot inject markup", () => {
        panel.show({ id: "n1", label: "Text", attributes: [], source: "<img onerror=x>" });
        expect(body.querySelector("pre code")?.textContent).toBe("<img onerror=x>");
        expect(body.querySelector("pre code img")).toBeNull();
    });

    it("notes that a synthetic node has no source of its own", () => {
        panel.show({ id: "n1", label: "Speaker (default)", attributes: [] });
        expect(body.querySelector(".inserted-note")?.textContent).toContain(
            "Inserted by the compiler",
        );
        expect(body.querySelector("pre code")).toBeNull();
    });

    it("marks recognized jump syntax only when the stage has Dialogue semantics", () => {
        const jumpNode: DisplayNode = {
            id: "n1",
            label: "Line",
            attributes: [],
            source: "=> [Go](#go)",
        };

        panel.show(jumpNode, { recognizeJumps: true });
        expect(body.querySelector(".preview .jump-ligature")?.textContent).toBe("=>");

        panel.show(jumpNode);
        expect(body.querySelector(".preview .jump-ligature")).toBeNull();
    });

    it("clears back to the placeholder", () => {
        panel.show({ id: "n1", label: "Heading", attributes: [], source: "# Scene" });
        panel.clear();

        expect(title.textContent).toBe("Node details");
        expect(body.textContent).toContain("Click any node");
    });

    describe("jump to source", () => {
        const jumpButton = () => title.querySelector<HTMLButtonElement>(".node-jump");

        it("offers no jump affordance when no jump handler is provided", () => {
            panel.show({
                id: "n1",
                label: "A",
                attributes: [],
                source: "# A",
                span: { start: 0, end: 3 },
            });
            expect(jumpButton()).toBeNull();
        });

        it("selects a real node's span from a button beside the title", () => {
            const jumps: Array<{ start: number; end: number }> = [];
            const jumping = createDetailPanel({ jumpToSource: (span) => jumps.push(span) });
            jumping.show({
                id: "n1",
                label: "Line",
                attributes: [],
                source: "Alice: Hi",
                span: { start: 5, end: 14 },
            });

            const button = jumpButton()!;
            expect(button.hidden).toBe(false);
            expect(button.getAttribute("aria-label")).toBe("Jump to source");
            button.click();
            expect(jumps).toEqual([{ start: 5, end: 14 }]);
        });

        it("places the caret for a synthetic node's zero-width span", () => {
            const jumps: Array<{ start: number; end: number }> = [];
            const jumping = createDetailPanel({ jumpToSource: (span) => jumps.push(span) });
            // A synthetic node has no source (shows the note) but carries a zero-width caret.
            jumping.show({
                id: "n1",
                label: "Speaker (default)",
                attributes: [],
                span: { start: 7, end: 7 },
            });

            const button = jumpButton()!;
            expect(button.hidden).toBe(false);
            button.click();
            expect(jumps).toEqual([{ start: 7, end: 7 }]);
        });

        it("hides the jump for a node with no span, and after clear", () => {
            const jumping = createDetailPanel({ jumpToSource: () => {} });
            jumping.show({ id: "n1", label: "Orphan", attributes: [] });
            expect(jumpButton()?.hidden).toBe(true);

            jumping.show({
                id: "n2",
                label: "Line",
                attributes: [],
                source: "Hi",
                span: { start: 0, end: 2 },
            });
            expect(jumpButton()?.hidden).toBe(false);

            jumping.clear();
            expect(jumpButton()?.hidden).toBe(true);
        });
    });

    it("lists what leads to a node and what it leads to, naming the route of each", () => {
        const node: DisplayNode = {
            id: "n1",
            label: "Guide: You are inside.",
            attributes: [],
            source: "Guide: You are inside.",
        };

        panel.show(node, {
            neighbors: {
                incoming: [
                    { id: "left", ownerId: "n1", label: "Alice: Left.", edgeCategory: "choice" },
                    { id: "right", ownerId: "n1", label: "Alice: Right.", edgeCategory: "break" },
                ],
                outgoing: [{ id: "end", ownerId: "n1", label: "End", edgeCategory: "break" }],
            },
        });

        expect([...body.querySelectorAll("h4")].map((h) => h.textContent)).toEqual([
            "Incoming",
            "Outgoing",
            "Source",
            "Preview",
        ]);
        expect(
            [...body.querySelectorAll("table.neighbors thead th")].map((t) => t.textContent),
        ).toEqual(["Edge", "Source", "Edge", "Destination"]);
        expect([...body.querySelectorAll("button.route")].map((t) => t.textContent)).toEqual([
            "Choice",
            "Succession",
            "Succession",
        ]);
        expect([...body.querySelectorAll("button.neighbor")].map((b) => b.textContent)).toEqual([
            "Alice: Left.",
            "Alice: Right.",
            "End",
        ]);
    });

    it("says so plainly when nothing leads to a node", () => {
        panel.show(
            { id: "n1", label: "Guide: Hello.", attributes: [] },
            { neighbors: { incoming: [], outgoing: [] } },
        );

        expect([...body.querySelectorAll(".neighbor-empty")].map((c) => c.textContent)).toEqual([
            "None",
            "None",
        ]);
    });

    it("omits the lists for a stage that does not describe a flow", () => {
        panel.show({ id: "n1", label: "Heading", attributes: [] });

        expect(body.querySelector("table.neighbors")).toBeNull();
    });

    it("takes the reader to the node a row names", () => {
        document.body.innerHTML = `<h2 id="detail-title"></h2><div id="detail-body"></div>`;
        body = document.getElementById("detail-body")!;
        const visited: string[] = [];
        const walking = createDetailPanel({ selectNode: (id) => visited.push(id) });

        walking.show(
            { id: "n1", label: "Guide: Hello.", attributes: [] },
            { neighbors: { incoming: [], outgoing: [{ id: "end", ownerId: "n1", label: "End" }] } },
        );
        body.querySelector<HTMLButtonElement>("button.neighbor")!.click();

        expect(visited).toEqual(["end"]);
    });

    it("escapes a neighbor's label, so a node cannot inject markup through the panel", () => {
        panel.show(
            { id: "n1", label: "Guide: Hello.", attributes: [] },
            {
                neighbors: {
                    incoming: [],
                    outgoing: [{ id: "x", ownerId: "n1", label: "<img src=x onerror=alert(1)>" }],
                },
            },
        );

        expect(body.querySelector("button.neighbor img")).toBeNull();
        expect(body.querySelector("button.neighbor")?.textContent).toBe(
            "<img src=x onerror=alert(1)>",
        );
    });

    it("shows a route: what it means, and the two nodes it joins", () => {
        panel.showEdge({
            category: "break",
            source: { id: "a", label: "Guide: Hello.", category: "speech" },
            target: { id: "b", label: "End", category: "terminal" },
        });

        expect(title.textContent).toBe("Succession");
        expect(body.querySelector(".route-meaning")?.textContent).toContain("natural order");
        expect([...body.querySelectorAll("table.neighbors th")].map((t) => t.textContent)).toEqual([
            "Source",
            "Destination",
        ]);
        expect([...body.querySelectorAll("button.neighbor")].map((b) => b.textContent)).toEqual([
            "Guide: Hello.",
            "End",
        ]);
    });

    it("lets a reader walk off an edge to either end of it", () => {
        document.body.innerHTML = `<h2 id="detail-title"></h2><div id="detail-body"></div>`;
        body = document.getElementById("detail-body")!;
        const visited: string[] = [];
        const walking = createDetailPanel({ selectNode: (id) => visited.push(id) });

        walking.showEdge({
            category: "break",
            source: { id: "a", label: "Guide: Hello." },
            target: { id: "b", label: "End" },
        });
        body.querySelectorAll<HTMLButtonElement>("button.neighbor")[1].click();

        expect(visited).toEqual(["b"]);
    });

    it("opens the route an edge cell names", () => {
        document.body.innerHTML = `<h2 id="detail-title"></h2><div id="detail-body"></div>`;
        body = document.getElementById("detail-body")!;
        const opened: string[] = [];
        const walking = createDetailPanel({
            selectEdge: (fromId, toId) => opened.push(`${fromId}->${toId}`),
        });

        walking.show(
            { id: "n1", label: "Guide: Hello.", attributes: [] },
            {
                neighbors: {
                    incoming: [{ id: "a", ownerId: "n1", label: "Before", edgeCategory: "break" }],
                    outgoing: [{ id: "b", ownerId: "n1", label: "After", edgeCategory: "jump" }],
                },
            },
        );
        const routes = body.querySelectorAll<HTMLButtonElement>("button.route");
        routes[0].click();
        routes[1].click();

        // An incoming row names the edge that arrives here; an outgoing one, the edge that leaves.
        expect(opened).toEqual(["a->n1", "n1->b"]);
    });

    it("names the region a node sits in, since the drawing names it only once", () => {
        panel.show({ id: "n1", label: "Guide: Hello.", attributes: [], region: "The Gate" });

        expect(body.querySelector("table")?.textContent).toContain("The Gate");
    });

    it("shows a region: how much it holds, and what crosses its border", () => {
        panel.showRegion(
            {
                name: "The Gate",
                nodeCount: 4,
                entering: [{ id: "out", ownerId: "in", label: "(jump)", edgeCategory: "jump" }],
                leaving: [{ id: "next", ownerId: "last", label: "End", edgeCategory: "break" }],
                tint: 0,
            },
            "# The Gate\n\n- Alice: Left.\n",
        );

        expect(title.textContent).toBe("The Gate");
        expect(body.querySelector("table")?.textContent).toContain("4");
        expect([...body.querySelectorAll("h4")].map((h) => h.textContent)).toEqual([
            "Entering",
            "Leaving",
            "Source",
            "Preview",
        ]);
        expect(body.querySelector(".preview li")).not.toBeNull();
    });

    it("leaves out a region's text when the report has no source to slice", () => {
        panel.showRegion({ name: "The Gate", nodeCount: 1, entering: [], leaving: [], tint: 1 });

        expect([...body.querySelectorAll("h4")].map((h) => h.textContent)).toEqual([
            "Entering",
            "Leaving",
        ]);
    });

    it("makes the region a node sits in a way into that region", () => {
        document.body.innerHTML = `<h2 id="detail-title"></h2><div id="detail-body"></div>`;
        body = document.getElementById("detail-body")!;
        const opened: string[] = [];
        const walking = createDetailPanel({ selectRegion: (name) => opened.push(name) });

        walking.show({ id: "n1", label: "Guide: Hello.", attributes: [], region: "The Gate" });
        body.querySelector<HTMLButtonElement>("button.region-link")!.click();

        expect(opened).toEqual(["The Gate"]);
    });
});
