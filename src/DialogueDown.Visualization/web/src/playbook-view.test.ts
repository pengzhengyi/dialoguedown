import { describe, it, expect, vi } from "vitest";
import { EditorView } from "@codemirror/view";
import { insertNewlineAndIndent } from "@codemirror/commands";
import { createPlaybookView } from "./playbook-view";
import type { PlaybookReport } from "./model";

/** Real enough to jump inside: two speakers, and a second node whose id is not its position. */
const JUMPABLE_JSON = `{
  "entry": 0,
  "speakers": [
    {
      "name": "Alice"
    },
    {
      "name": "(anonymous)"
    }
  ],
  "nodes": [
    {
      "kind": "line",
      "id": 0
    },
    {
      "kind": "line",
      "id": 9
    }
  ]
}`;

/** A compiled playbook with a named speaker, an anonymous default, and one tag. */
function compiled(): PlaybookReport {
    return {
        json: JUMPABLE_JSON,
        metadata: {
            script: "scene.dialogue.md",
            formatVersion: 0,
            schemaUrl: "https://pengzhengyi.github.io/dialoguedown/schema/playbook-0.schema.json",
            requires: ["core"],
            uses: [],
            entry: 0,
            nodeCount: 6,
            anchorCount: 2,
        },
        anchors: [{ name: "the-tavern", node: 9 }],
        speakers: [
            {
                id: "alice",
                name: "Alice",
                default: false,
                tags: [{ name: "role", value: "guide", reserved: false }],
            },
            { default: true, tags: [] },
        ],
        nodes: [
            {
                id: 0,
                kind: "line",
                category: "speech",
                segments: [
                    { text: "Alice", role: "speaker" },
                    { text: ": ", role: "separator" },
                    { text: "Which way?", role: "plain" },
                ],
                targets: [1],
            },
            {
                id: 1,
                kind: "choice",
                category: "structure",
                segments: [
                    { text: "", role: "boundary" },
                    { text: "Go left", role: "plain", target: 2 },
                    { text: " || ", role: "boundary" },
                    { text: "Go right", role: "plain", target: 9 },
                ],
                targets: [2, 9],
            },
            {
                id: 2,
                kind: "control",
                category: "call",
                segments: [{ text: "ShowBackground(tavern, firelit)", role: "command" }],
                targets: [9],
            },
            {
                id: 9,
                kind: "branch",
                category: "structure",
                segments: [
                    { text: "IF ", role: "keyword" },
                    { text: "Hero.IsBrave", role: "plain" },
                    { text: " THEN ", role: "keyword" },
                    { text: "10", role: "plain" },
                    { text: " ELSE ", role: "keyword" },
                    { text: "11", role: "plain" },
                ],
                targets: [10, 11],
            },
            {
                id: 10,
                kind: "end",
                category: "terminal",
                segments: [{ text: "END", role: "keyword" }],
                targets: [],
            },
        ],
    };
}

/** One named table panel in the right pane. */
function panel(view: HTMLElement, title: string): HTMLElement | undefined {
    return [...view.querySelectorAll<HTMLElement>(".table-panel")].find(
        (candidate) => candidate.querySelector(".table-panel-title")?.textContent === title,
    );
}

/** The body rows of one named table panel. */
function bodyRows(view: HTMLElement, title: string): HTMLTableRowElement[] {
    return [...(panel(view, title)?.querySelectorAll<HTMLTableRowElement>("tbody tr") ?? [])];
}

describe("createPlaybookView", () => {
    it("shows the serialized playbook in a read-only editor", () => {
        const view = createPlaybookView(compiled());

        const editor = view.querySelector(".playbook-source .cm-editor");
        expect(editor).not.toBeNull();
        expect(view.querySelector(".playbook-source")?.textContent).toContain('"entry"');
    });

    it("refuses a reader's edit — the playbook is compiled, not authored", () => {
        const view = createPlaybookView(compiled());
        const editor = EditorView.findFromDOM(view.querySelector(".playbook-source .cm-editor")!)!;
        const before = editor.state.doc.toString();
        editor.dispatch({ selection: { anchor: 5 } });

        // The command an editing keystroke runs through: it consults `readOnly` and declines.
        expect(insertNewlineAndIndent(editor)).toBe(false);

        expect(editor.state.doc.toString()).toBe(before);
        expect(
            view.querySelector(".playbook-source .cm-content")?.getAttribute("aria-readonly"),
        ).toBe("true");
    });

    it("summarizes the playbook's header as a field/value panel", () => {
        const view = createPlaybookView(compiled());

        const text = panel(view, "Playbook")?.textContent ?? "";
        expect(text).toContain("scene.dialogue.md");
        expect(text).toContain("core");
        expect(text).toContain("6");
    });

    it("links out to the published schema the playbook names", () => {
        const view = createPlaybookView(compiled());

        const link = view.querySelector<HTMLAnchorElement>(".playbook-schema-link");
        expect(link?.href).toBe(
            "https://pengzhengyi.github.io/dialoguedown/schema/playbook-0.schema.json",
        );
        expect(link?.textContent).toBe("playbook-0.schema.json");
        // It leaves the report, so it must not hand the opener a window handle.
        expect(link?.rel).toBe("noopener noreferrer");
    });

    it("says nothing for a header list the playbook did not fill", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Playbook");
        const uses = rows.find((row) => row.cells[0]?.textContent === "Uses");

        expect(uses?.cells[1]?.textContent).toBe("");
    });

    it("names the anonymous default speaker, whose namelessness is the point", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Speakers");

        expect(rows).toHaveLength(2);
        expect(rows[0].textContent).toContain("Alice");
        expect(rows[0].textContent).toContain("alice");
        expect(rows[1].cells[0]?.textContent).toBe("(anonymous)");
    });

    it("ticks the default speaker and leaves the others' cell empty", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Speakers");

        expect(rows[0].cells[3]?.textContent).toBe("");
        expect(rows[1].cells[3]?.textContent).toBe("✓");
    });

    it("draws each tag as a capsule carrying the text to copy", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Speakers");
        const chip = rows[0].cells[2]?.querySelector<HTMLElement>(".dd-tag");

        expect(chip?.dataset.copy).toBe("#role=guide");
        expect(chip?.classList.contains("dd-tag-custom")).toBe(true);
        // The identity dot is what tells one writer-invented tag from the next.
        expect(chip?.querySelector(".dd-tag-dot")).not.toBeNull();
    });

    it("copies an @id and an anchor, the identifiers a writer pastes into a script", () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
        const view = createPlaybookView(compiled());

        bodyRows(view, "Speakers")[0].cells[1]?.dispatchEvent(
            new MouseEvent("click", { bubbles: true }),
        );
        expect(writeText).toHaveBeenCalledExactlyOnceWith("@alice");

        writeText.mockClear();
        bodyRows(view, "Anchors")[0].cells[0]?.dispatchEvent(
            new MouseEvent("click", { bubbles: true }),
        );
        expect(writeText).toHaveBeenCalledExactlyOnceWith("#the-tavern");
    });

    it("offers a node's number, a speaker's name, and the entry node as jumps", () => {
        // Where the click *lands* is the resolver's business and is tested against a document in
        // playbook-jump.test.ts; what matters here is that the right cells carry the right target.
        const view = createPlaybookView(compiled());

        expect(bodyRows(view, "Anchors")[0].cells[1]?.dataset.jump).toBe('{"kind":"node","id":9}');
        expect(bodyRows(view, "Speakers")[1].cells[0]?.dataset.jump).toBe(
            '{"kind":"speaker","index":1}',
        );
        const entry = bodyRows(view, "Playbook").find(
            (row) => row.cells[0]?.textContent === "Entry node",
        );
        expect(entry?.cells[1]?.dataset.jump).toBe('{"kind":"node","id":0}');
    });

    it("binds a speaker's jump to its place in the array, not to its row", () => {
        // The panels sort and filter, so a row's position is not the speaker's index. Binding at
        // build time is what keeps a sorted table pointing at the right object.
        const view = createPlaybookView(compiled());
        const rows = bodyRows(view, "Speakers");

        expect(rows.map((row) => row.cells[0]?.dataset.jump)).toEqual([
            '{"kind":"speaker","index":0}',
            '{"kind":"speaker","index":1}',
        ]);
    });

    it("leaves prose alone, so only a place in the document is a destination", () => {
        const view = createPlaybookView(compiled());
        const script = bodyRows(view, "Playbook").find(
            (row) => row.cells[0]?.textContent === "Script",
        );

        expect(script?.cells[1]?.dataset.jump).toBeUndefined();
        expect(bodyRows(view, "Anchors")[0].cells[0]?.dataset.jump).toBeUndefined();
    });

    it("copies a tag when it is clicked, the same as the Config tab", () => {
        // The capsule wears a hover ring and carries the text to copy, so it promises a click
        // will work. That promise is the shared table's to keep, not the Config tab's alone.
        const writeText = vi.fn().mockResolvedValue(undefined);
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
        const rows = bodyRows(createPlaybookView(compiled()), "Speakers");

        rows[0].cells[2]
            ?.querySelector<HTMLElement>(".dd-tag")
            ?.dispatchEvent(new MouseEvent("click", { bubbles: true }));

        // Exactly once: the table panel wires the copying, so the view must not wire it again.
        expect(writeText).toHaveBeenCalledExactlyOnceWith("#role=guide");
    });

    it("leaves an absent id and an empty tag list as empty cells", () => {
        // Nothing to say, so the table says nothing: the reader's eye goes to the speakers that
        // do carry an id or a tag, not to a column of placeholders.
        const rows = bodyRows(createPlaybookView(compiled()), "Speakers");

        // Written with its `@`, exactly as a script references it.
        expect(rows[0].cells[1]?.textContent).toBe("@alice");
        // Written with its `#`, exactly as a script writes it and as the other two tabs show it.
        expect(rows[0].cells[2]?.textContent).toBe("#role=guide");
        expect(rows[1].cells[1]?.textContent).toBe("");
        expect(rows[1].cells[2]?.textContent).toBe("");
    });

    it("lists every anchor a jump may name, with the node it lands on", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Anchors");

        expect(rows).toHaveLength(1);
        // Written with its `#`, exactly as a jump names it.
        expect(rows[0].cells[0]?.textContent).toBe("#the-tavern");
        expect(rows[0].cells[1]?.textContent).toBe("9");
    });

    it("gives each table the report's panel chrome — a count, a caret, and a search", () => {
        const view = createPlaybookView(compiled());

        expect([...view.querySelectorAll(".table-panel-title")].map((t) => t.textContent)).toEqual([
            "Playbook",
            "Speakers",
            "Anchors",
            "Nodes",
        ]);
        expect(panel(view, "Speakers")?.querySelector(".table-panel-count")?.textContent).toBe("2");
        expect(panel(view, "Speakers")?.querySelector(".table-panel-search")).not.toBeNull();
        expect(panel(view, "Speakers")?.querySelector(".table-panel-toggle")).not.toBeNull();
    });

    it("explains why there is no playbook instead of showing an empty editor", () => {
        const view = createPlaybookView({
            anchors: [],
            speakers: [],
            nodes: [],
            unavailable: "The compile did not reach a playbook.",
        });

        expect(view.querySelector(".playbook-source .cm-editor")).toBeNull();
        expect(view.querySelector(".playbook-empty-state")?.textContent).toContain(
            "did not reach a playbook",
        );
        expect(panel(view, "Speakers")?.textContent).toContain("no speakers");
        expect(panel(view, "Anchors")?.textContent).toContain("jumped to by name");
    });

    it("gives the tables panel its own collapse toggle and split", () => {
        const view = createPlaybookView(compiled());

        expect(view.querySelector(".playbook-divider .collapse-toggle")).not.toBeNull();
        expect(view.querySelector(".playbook-side")).not.toBeNull();
    });
});

describe("createPlaybookView Nodes table", () => {
    /** The header cell of a named column, where its sort button and any facet control live. */
    function headerCell(view: HTMLElement, column: string): HTMLTableCellElement {
        const headers = panel(view, "Nodes")?.querySelectorAll<HTMLTableCellElement>("thead th");
        return [...(headers ?? [])].find(
            (th) => th.querySelector(".th-sort")?.textContent === column,
        )!;
    }

    it("is titled Nodes, with the columns a reader follows a playbook by", () => {
        const view = createPlaybookView(compiled());
        const table = panel(view, "Nodes");

        expect(table).toBeDefined();
        expect([...table!.querySelectorAll("thead th")].map((th) => th.textContent)).toEqual([
            "#",
            "Kind",
            "Summary",
            "Leads to",
        ]);
    });

    it("gives every node a row, and reads its own id rather than its place in the list", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Nodes");

        // Node 9 is the fourth entry but holds a sparse id, so a table that counted positions
        // would mislabel it.
        expect(rows).toHaveLength(5);
        expect(rows.map((row) => row.cells[0]?.textContent)).toEqual(["0", "1", "2", "9", "10"]);
    });

    it("shows what each node holds", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Nodes");

        expect(rows[0].cells[2]?.textContent).toBe("Alice: Which way?");
        expect(rows[3].cells[2]?.textContent).toBe("IF Hero.IsBrave THEN 10 ELSE 11");
    });

    it("colors a node's Kind with the category the Dialogue Graph gives it", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Nodes");

        expect(rows.map((row) => row.cells[1]?.dataset.category)).toEqual([
            "speech",
            "structure",
            "call",
            "structure",
            "terminal",
        ]);
    });

    it("makes the whole cell the target when a node leads only one way", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Nodes");

        expect(rows[0].cells[3]?.dataset.jump).toBe('{"kind":"node","id":1}');
    });

    it("offers each of several ways out on its own, rather than picking one", () => {
        const cell = bodyRows(createPlaybookView(compiled()), "Nodes")[3].cells[3];

        // The cell as a whole is not the target: it names two places, and a single link would
        // announce both and deliver one.
        expect(cell?.dataset.jump).toBeUndefined();
        expect(cell?.textContent).toBe("10, 11");

        const ways = [...(cell?.querySelectorAll<HTMLElement>("[data-jump]") ?? [])];
        expect(ways.map((way) => way.dataset.jump)).toEqual([
            '{"kind":"node","id":10}',
            '{"kind":"node","id":11}',
        ]);
        // Each is a real button, so a keyboard reaches every destination the cell names.
        expect(ways.map((way) => way.tagName)).toEqual(["BUTTON", "BUTTON"]);
    });

    it("leaves the way-out column empty for a node that ends the run", () => {
        const rows = bodyRows(createPlaybookView(compiled()), "Nodes");

        expect(rows[4].cells[3]?.textContent).toBe("");
        expect(rows[4].cells[3]?.dataset.jump).toBeUndefined();
    });

    it("offers a faceted filter on Kind, the question this table makes answerable", () => {
        const view = createPlaybookView(compiled());

        const facet = headerCell(view, "Kind").querySelector(".th-facet");
        expect(facet).not.toBeNull();
        expect(facet?.getAttribute("aria-label")).toBe("Filter by Kind");
        // Only a categorical column carries a funnel; a free-text column is the search box's.
        expect(headerCell(view, "Summary").querySelector(".th-facet")).toBeNull();
    });

    it("draws each role in its own class, and a writer's words in none", () => {
        const report = compiled();
        report.nodes[4].segments = [
            { text: "Spoke", role: "speaker" },
            { text: "IF ", role: "keyword" },
            { text: ": ", role: "separator" },
            { text: "(fade)", role: "command" },
            { text: "{Key}", role: "query" },
            { text: "<no label>", role: "absent" },
            { text: "plain words", role: "plain" },
        ];

        const cell = bodyRows(createPlaybookView(report), "Nodes")[4].cells[2];

        expect(cell?.textContent).toBe("SpokeIF : (fade){Key}<no label>plain words");
        // A role with no class would draw uncoloured here rather than fail anywhere else.
        expect([...cell!.querySelectorAll("span")].map((span) => span.className)).toEqual([
            "dd-sum-speaker",
            "dd-sum-keyword",
            "dd-sum-separator",
            "dd-sum-command",
            "dd-sum-query",
            "dd-sum-absent",
        ]);
    });

    it("draws exactly the segments it was sent, reading nothing back out of the text", () => {
        // The bug this change removes: a writer's own separator used to be split as grammar. The
        // client can no longer do that — it draws the roles it was handed and nothing else.
        const report = compiled();
        report.nodes[4].segments = [
            { text: "Go left || right", role: "plain" },
            { text: " || ", role: "separator" },
            { text: "Go right", role: "plain" },
        ];

        const cell = bodyRows(createPlaybookView(report), "Nodes")[4].cells[2];

        expect(cell?.textContent).toBe("Go left || right || Go right");
        expect(
            [...cell!.querySelectorAll(".dd-sum-separator")].map((piece) => piece.textContent),
        ).toEqual([" || "]);
    });

    // A menu is a list of options, and the boundaries the projection placed are where one ends and
    // the next begins. The client draws that structure with the list's own marker instead of the
    // punctuation — which is the freedom the roles bought.
    it("draws a choice as a list, one option to a line", () => {
        const cell = bodyRows(createPlaybookView(compiled()), "Nodes")[1].cells[2];

        expect([...cell!.querySelectorAll("li")].map((item) => item.textContent)).toEqual([
            "Go left",
            "Go right",
        ]);
        // Nothing draws the punctuation: it is the boundary, not a glyph.
        expect(cell?.querySelector(".dd-sum-separator")).toBeNull();
        // The cell's text is its lines, so search, sort, and a copy still read both options.
        expect(cell?.textContent).toBe("Go left\nGo right");
    });

    // A menu's options are the picks on offer, so each one is a way to where it leads, and its tip
    // says which pick it is rather than leaving the reader to match it against the Leads to column.
    it("offers a menu's options as jumps to where they lead", () => {
        const cell = bodyRows(createPlaybookView(compiled()), "Nodes")[1].cells[2];
        const options = [...cell!.querySelectorAll<HTMLElement>("button.dd-jump")];

        expect(options.map((option) => option.textContent)).toEqual(["Go left", "Go right"]);
        expect(options.map((option) => JSON.parse(option.dataset.jump ?? "null"))).toEqual([
            { kind: "node", id: 2 },
            { kind: "node", id: 9 },
        ]);
        expect(options[0]?.getAttribute("data-tip")).toContain("Choice");
        expect(options[0]?.getAttribute("data-tip")).toContain("Click to reveal");
    });

    // A piece whose own role means something keeps saying it, and adds only that it can be followed:
    // a query inside an option's words is still a question for the game.
    it("keeps a query's own meaning when the option it sits in leads somewhere", () => {
        const report = compiled();
        report.nodes[1].segments = [
            { text: "", role: "boundary" },
            { text: "Spend ", role: "plain", target: 2 },
            { text: "{Gold}", role: "query", target: 2 },
            { text: " || ", role: "boundary" },
            { text: "Wait", role: "plain", target: 9 },
        ];

        const query = bodyRows(createPlaybookView(report), "Nodes")[1].cells[2].querySelector(
            ".dd-sum-query",
        );

        expect(query?.getAttribute("data-tip")).toContain("Query");
        expect(query?.getAttribute("data-tip")).toContain("Click to reveal");
    });

    it("draws a menu of one as a line, because one option is not a list", () => {
        const report = compiled();
        report.nodes[1].segments = [{ text: "Only way on", role: "plain" }];

        const cell = bodyRows(createPlaybookView(report), "Nodes")[1].cells[2];

        expect(cell?.querySelector("li")).toBeNull();
        expect(cell?.textContent).toBe("Only way on");
    });

    // A control's items are the steps it takes, so their order is meaning: the list is numbered,
    // and the boundaries the projection placed become the numbers.
    it("draws a control's commands as an ordered list", () => {
        const report = compiled();
        report.nodes[2].segments = [
            { text: "", role: "boundary" },
            { text: "ShowBackground(tavern, firelit)", role: "command" },
            { text: "; ", role: "boundary" },
            { text: "PlaySound(fire)", role: "command" },
        ];

        const cell = bodyRows(createPlaybookView(report), "Nodes")[2].cells[2];

        expect(cell?.querySelector("ol")).not.toBeNull();
        expect([...cell!.querySelectorAll("li")].map((item) => item.textContent)).toEqual([
            "ShowBackground(tavern, firelit)",
            "PlaySound(fire)",
        ]);
        // The cell's text is its lines, so search, sort, and a copy still read every command.
        expect(cell?.textContent).toBe("ShowBackground(tavern, firelit)\nPlaySound(fire)");
    });

    // A list can be subject to a condition, and the condition introduces the list rather than
    // becoming its first item: it reads on the cell's first line.
    it("reads a conditional command list's condition before the list", () => {
        const report = compiled();
        report.nodes[2].segments = [
            { text: "IF ", role: "keyword" },
            { text: "Hero.IsBrave", role: "plain" },
            { text: " THEN ", role: "keyword" },
            { text: "", role: "boundary" },
            { text: "(fade)", role: "command" },
            { text: "; ", role: "boundary" },
            { text: "(wait)", role: "command" },
        ];

        const cell = bodyRows(createPlaybookView(report), "Nodes")[2].cells[2];

        expect(cell?.textContent).toBe("IF Hero.IsBrave THEN \n(fade)\n(wait)");
        expect([...cell!.querySelectorAll("li")].map((item) => item.textContent)).toEqual([
            "(fade)",
            "(wait)",
        ]);
    });

    // A reader who has not learned the script language cannot tell a query from braces a writer
    // typed, so the piece says which it is on hover — the way the graph's routes explain themselves.
    it("explains a query on hover", () => {
        const report = compiled();
        report.nodes[4].segments = [{ text: "{Gold}", role: "query" }];

        const piece = bodyRows(createPlaybookView(report), "Nodes")[4].cells[2].querySelector(
            ".dd-sum-query",
        );

        expect(piece?.getAttribute("data-tip")).toContain("Query");
        expect(piece?.getAttribute("data-tip")).toContain("Answered while the game runs");
    });

    // A piece that names a node is a way to reach it, so it is a control rather than a stretch of
    // text: it carries the jump, the key that lights the node up elsewhere, and what it means.
    it("offers a branch arm's target as a jump", () => {
        const report = compiled();
        report.nodes[3].segments = [
            { text: "IF ", role: "keyword" },
            { text: "DoorIsHot", role: "plain" },
            { text: " THEN ", role: "keyword" },
            { text: "10", role: "target", target: 10 },
        ];

        const piece = bodyRows(createPlaybookView(report), "Nodes")[3].cells[2].querySelector(
            "button",
        );

        expect(piece?.tagName).toBe("BUTTON");
        expect(piece?.textContent).toBe("10");
        expect(JSON.parse(piece?.dataset.jump ?? "null")).toEqual({ kind: "node", id: 10 });
        expect(piece?.getAttribute("data-ref-key")).toBe("node:10");
        expect(piece?.getAttribute("data-tip")).toContain("Conditional");
        expect(piece?.getAttribute("aria-label")).toBe("Reveal 10 in the playbook");
    });

    it("offers a divert's own words as a jump", () => {
        const report = compiled();
        report.nodes[2].segments = [
            { text: "⇒ ", role: "separator" },
            { text: "The Mountain Road", role: "target", target: 7 },
        ];

        const piece = bodyRows(createPlaybookView(report), "Nodes")[2].cells[2].querySelector(
            "button",
        );

        expect(piece?.textContent).toBe("The Mountain Road");
        expect(JSON.parse(piece?.dataset.jump ?? "null")).toEqual({ kind: "node", id: 7 });
        expect(piece?.getAttribute("data-tip")).toContain("Jump");
        expect(piece?.getAttribute("data-tip")).toContain("Click to reveal");
    });
});
