import { describe, it, expect, beforeEach } from "vitest";
import { createTablePanel } from "./semantic-table";
import type { SemanticTable } from "./model";

function speakerTable(): SemanticTable {
    return {
        title: "Speakers",
        columns: ["Name", "Id"],
        emptyText: "No speakers are declared.",
        rows: [
            {
                entityKey: "speaker:@guide",
                cells: [
                    { text: "Guide", entityKey: "speaker:@guide", category: "speech" },
                    { text: "@guide" },
                ],
            },
        ],
    };
}

function jumpTable(): SemanticTable {
    return {
        title: "Jump resolutions",
        columns: ["Label", "Target", "Resolves to"],
        emptyText: "No jumps appear in this script.",
        rows: [
            {
                cells: [
                    { text: "Enter" },
                    { text: "#the-market" },
                    { text: "The market", refKey: "scene:the-market", category: "jump" },
                ],
            },
        ],
    };
}

function emptyTable(): SemanticTable {
    return { title: "Anchors", columns: ["Anchor", "Scene"], emptyText: "No anchors.", rows: [] };
}

function castTable(): SemanticTable {
    return {
        title: "Speakers",
        columns: ["Name", "Id"],
        emptyText: "No speakers are declared.",
        rows: [
            { cells: [{ text: "Charlie" }, { text: "@charlie" }] },
            { cells: [{ text: "Alice" }, { text: "@alice" }], entityKey: "speaker:@alice" },
            { cells: [{ text: "Bob" }, { text: "@bob" }] },
        ],
    };
}

/** The visible text of each body row's first cell, in render order. */
function firstColumn(panel: HTMLElement): string[] {
    return [...panel.querySelectorAll("tbody tr")].map(
        (tr) => tr.querySelector("td")?.textContent ?? "",
    );
}

function setFilter(panel: HTMLElement, value: string): void {
    const search = panel.querySelector<HTMLInputElement>("input.table-search")!;
    search.value = value;
    search.dispatchEvent(new Event("input"));
}

function jumpFacetTable(): SemanticTable {
    return {
        title: "Jump resolutions",
        columns: ["Type", "Jump"],
        emptyText: "No jumps.",
        facetColumns: ["Type"],
        rows: [
            { cells: [{ text: "Scene" }, { text: "east" }] },
            { cells: [{ text: "End" }, { text: "the end" }] },
            { cells: [{ text: "Scene" }, { text: "west" }] },
        ],
    };
}

function chooseFacet(panel: HTMLElement, value: string): void {
    const select = panel.querySelector<HTMLSelectElement>(".table-facet select")!;
    select.value = value;
    select.dispatchEvent(new Event("change"));
}

describe("createTablePanel", () => {
    beforeEach(() => {
        document.body.innerHTML = "";
    });

    it("renders the title, the row count, and one column header per column", () => {
        const panel = createTablePanel(speakerTable());

        expect(panel.querySelector(".table-panel-title")?.textContent).toBe("Speakers");
        expect(panel.querySelector(".table-panel-count")?.textContent).toBe("1");
        expect([...panel.querySelectorAll("th")].map((th) => th.textContent)).toEqual([
            "Name",
            "Id",
        ]);
    });

    it("renders each cell's text and carries the row's entity key", () => {
        const panel = createTablePanel(speakerTable());
        const row = panel.querySelector("tbody tr");

        expect(row?.getAttribute("data-entity-key")).toBe("speaker:@guide");
        expect([...(row?.querySelectorAll("td") ?? [])].map((td) => td.textContent)).toEqual([
            "Guide",
            "@guide",
        ]);
    });

    it("tags an entity cell with its key and category", () => {
        const panel = createTablePanel(speakerTable());
        const cell = panel.querySelector("td");

        expect(cell?.getAttribute("data-entity-key")).toBe("speaker:@guide");
        expect(cell?.dataset.category).toBe("speech");
    });

    it("tags a reference cell with its ref key so the highlighter can cross-link it", () => {
        const panel = createTablePanel(jumpTable());
        const resolved = panel.querySelectorAll("td")[2];

        expect(resolved?.getAttribute("data-ref-key")).toBe("scene:the-market");
    });

    it("shows the empty note instead of a table when there are no rows", () => {
        const panel = createTablePanel(emptyTable());

        expect(panel.querySelector("table")).toBeNull();
        expect(panel.querySelector(".table-empty")?.textContent).toBe("No anchors.");
        expect(panel.querySelector(".table-panel-count")?.textContent).toBe("0");
    });

    it("collapses and reopens the panel when its header is pressed", () => {
        const panel = createTablePanel(speakerTable());
        const header = panel.querySelector<HTMLButtonElement>(".table-panel-header")!;
        expect(panel.classList.contains("collapsed")).toBe(false);
        expect(header.getAttribute("aria-expanded")).toBe("true");

        header.click();
        expect(panel.classList.contains("collapsed")).toBe(true);
        expect(header.getAttribute("aria-expanded")).toBe("false");

        header.click();
        expect(panel.classList.contains("collapsed")).toBe(false);
        expect(header.getAttribute("aria-expanded")).toBe("true");
    });
});

describe("createTablePanel — sorting and filtering", () => {
    beforeEach(() => {
        document.body.innerHTML = "";
    });

    it("gives the filter input an accessible label naming its table", () => {
        const panel = createTablePanel(castTable());
        const search = panel.querySelector<HTMLInputElement>("input.table-search");

        expect(search?.getAttribute("aria-label")).toBe("Filter Speakers");
    });

    it("sorts a column ascending, then descending, then back to document order", () => {
        const panel = createTablePanel(castTable());
        const button = panel.querySelector<HTMLButtonElement>("th .th-sort")!;
        const th = button.closest("th")!;

        expect(firstColumn(panel)).toEqual(["Charlie", "Alice", "Bob"]);
        expect(th.getAttribute("aria-sort")).toBe("none");

        button.click();
        expect(firstColumn(panel)).toEqual(["Alice", "Bob", "Charlie"]);
        expect(th.getAttribute("aria-sort")).toBe("ascending");

        button.click();
        expect(firstColumn(panel)).toEqual(["Charlie", "Bob", "Alice"]);
        expect(th.getAttribute("aria-sort")).toBe("descending");

        button.click();
        expect(firstColumn(panel)).toEqual(["Charlie", "Alice", "Bob"]);
        expect(th.getAttribute("aria-sort")).toBe("none");
    });

    it("filters rows by a case-insensitive substring across all columns", () => {
        const panel = createTablePanel(castTable());

        setFilter(panel, "ali"); // Alice's name
        expect(firstColumn(panel)).toEqual(["Alice"]);

        setFilter(panel, "@BOB"); // Bob's id, case-insensitively
        expect(firstColumn(panel)).toEqual(["Bob"]);

        setFilter(panel, "");
        expect(firstColumn(panel)).toEqual(["Charlie", "Alice", "Bob"]);
    });

    it("updates the panel count to the number of visible rows while filtering", () => {
        const panel = createTablePanel(castTable());
        const count = panel.querySelector(".table-panel-count")!;
        expect(count.textContent).toBe("3");

        setFilter(panel, "b"); // Bob only
        expect(count.textContent).toBe("1");

        setFilter(panel, "");
        expect(count.textContent).toBe("3");
    });

    it("shows a single no-matches note when the filter excludes every row", () => {
        const panel = createTablePanel(castTable());

        setFilter(panel, "zzz");
        expect(panel.querySelector(".table-nomatch")?.textContent).toBe("No matches.");
        expect(panel.querySelector(".table-panel-count")?.textContent).toBe("0");
    });

    it("keeps each row's cross-link key after a sort", () => {
        const panel = createTablePanel(castTable());
        panel.querySelector<HTMLButtonElement>("th .th-sort")!.click(); // sort by name

        const alice = [...panel.querySelectorAll("tbody tr")].find(
            (tr) => tr.querySelector("td")?.textContent === "Alice",
        );
        expect(alice?.getAttribute("data-entity-key")).toBe("speaker:@alice");
    });
});

describe("createTablePanel — faceted filters", () => {
    beforeEach(() => {
        document.body.innerHTML = "";
    });

    it("offers a labeled facet select per categorical column with All plus its distinct values", () => {
        const panel = createTablePanel(jumpFacetTable());
        const select = panel.querySelector<HTMLSelectElement>(".table-facet select")!;

        expect(select.getAttribute("aria-label")).toBe("Filter by Type");
        expect([...select.options].map((option) => option.textContent)).toEqual([
            "All",
            "Scene",
            "End",
        ]);
    });

    it("filters rows to a chosen facet value and clears with All", () => {
        const panel = createTablePanel(jumpFacetTable());

        chooseFacet(panel, "End");
        expect(firstColumn(panel)).toEqual(["End"]);

        chooseFacet(panel, ""); // All
        expect(firstColumn(panel)).toEqual(["Scene", "End", "Scene"]);
    });

    it("combines a facet with the free-text search", () => {
        const panel = createTablePanel(jumpFacetTable());

        chooseFacet(panel, "Scene"); // two Scene rows
        setFilter(panel, "west"); // only the "west" one
        expect(panel.querySelectorAll("tbody tr")).toHaveLength(1);
        expect(firstColumn(panel)).toEqual(["Scene"]);
    });

    it("renders no facet bar for a table with no categorical columns", () => {
        const panel = createTablePanel(castTable());

        expect(panel.querySelector(".table-facets")).toBeNull();
    });
});
