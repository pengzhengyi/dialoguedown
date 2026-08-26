import { describe, it, expect, beforeEach, vi } from "vitest";
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
    const control = panel.querySelector<HTMLButtonElement>(".th-facet")!;
    // Open the popover (tippy mounts it to the body), then pick the radio.
    (control as unknown as { _tippy: { show: () => void } })._tippy.show();
    const radio = document.querySelector<HTMLInputElement>(
        `.facet-popover input[value="${value}"]`,
    )!;
    radio.checked = true;
    radio.dispatchEvent(new Event("change"));
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

    it("collapses and reopens the panel when its caret toggle is pressed", () => {
        const panel = createTablePanel(speakerTable());
        const toggle = panel.querySelector<HTMLButtonElement>(".table-panel-toggle")!;
        expect(panel.classList.contains("collapsed")).toBe(false);
        expect(toggle.getAttribute("aria-expanded")).toBe("true");

        toggle.click();
        expect(panel.classList.contains("collapsed")).toBe(true);
        expect(toggle.getAttribute("aria-expanded")).toBe("false");

        toggle.click();
        expect(panel.classList.contains("collapsed")).toBe(false);
        expect(toggle.getAttribute("aria-expanded")).toBe("true");
    });
});

describe("createTablePanel — search reveal", () => {
    beforeEach(() => {
        document.body.innerHTML = "";
    });

    it("reveals the hidden search box when the magnifier toggle is pressed", () => {
        const panel = createTablePanel(castTable());
        const row = panel.querySelector<HTMLElement>(".table-search-row")!;
        const toggle = panel.querySelector<HTMLButtonElement>(".table-panel-search")!;

        expect(row.hidden).toBe(true);
        expect(toggle.getAttribute("aria-expanded")).toBe("false");

        toggle.click();
        expect(row.hidden).toBe(false);
        expect(toggle.getAttribute("aria-expanded")).toBe("true");
    });

    it("marks the search toggle active while the filter carries text", () => {
        const panel = createTablePanel(castTable());
        const toggle = panel.querySelector<HTMLButtonElement>(".table-panel-search")!;

        setFilter(panel, "ali");
        expect(toggle.classList.contains("active")).toBe(true);

        setFilter(panel, "");
        expect(toggle.classList.contains("active")).toBe(false);
    });

    it("has no search toggle for an empty table", () => {
        const panel = createTablePanel(emptyTable());

        expect(panel.querySelector(".table-panel-search")).toBeNull();
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

    it("offers a labeled facet popover with All plus the column's distinct values", () => {
        const panel = createTablePanel(jumpFacetTable());
        const control = panel.querySelector<HTMLButtonElement>(".th-facet")!;
        expect(control.getAttribute("aria-label")).toBe("Filter by Type");

        (control as unknown as { _tippy: { show: () => void } })._tippy.show();
        const popover = document.querySelector(".facet-popover")!;
        expect(popover.getAttribute("aria-label")).toBe("Filter by Type");
        expect(
            [...popover.querySelectorAll(".facet-option span")].map((s) => s.textContent),
        ).toEqual(["All", "Scene", "End"]);
    });

    it("filters rows to a chosen facet value and clears with All", () => {
        const panel = createTablePanel(jumpFacetTable());

        chooseFacet(panel, "End");
        expect(firstColumn(panel)).toEqual(["End"]);

        chooseFacet(panel, ""); // All
        expect(firstColumn(panel)).toEqual(["Scene", "End", "Scene"]);
    });

    it("shows the chosen facet value as a chip in the header", () => {
        const panel = createTablePanel(jumpFacetTable());

        chooseFacet(panel, "End");
        const control = panel.querySelector(".th-facet")!;
        expect(control.classList.contains("active")).toBe(true);
        expect(control.querySelector(".th-facet-value")?.textContent).toBe("End");
    });

    it("combines a facet with the free-text search", () => {
        const panel = createTablePanel(jumpFacetTable());

        chooseFacet(panel, "Scene"); // two Scene rows
        setFilter(panel, "west"); // only the "west" one
        expect(panel.querySelectorAll("tbody tr")).toHaveLength(1);
        expect(firstColumn(panel)).toEqual(["Scene"]);
    });

    it("renders no facet control for a table with no categorical columns", () => {
        const panel = createTablePanel(castTable());

        expect(panel.querySelector(".th-facet")).toBeNull();
    });
});

/** The match toggle button (Match case / Match whole word) by its accessible label. */
function toggle(panel: HTMLElement, label: string): HTMLButtonElement {
    return panel.querySelector<HTMLButtonElement>(`.dd-search-toggle[aria-label="${label}"]`)!;
}

/** The text of every highlight mark in the panel, in document order. */
function marks(panel: HTMLElement): string[] {
    return [...panel.querySelectorAll("mark.table-mark")].map((m) => m.textContent ?? "");
}

describe("createTablePanel — match highlighting and options", () => {
    beforeEach(() => {
        document.body.innerHTML = "";
    });

    it("wraps each match in a mark while leaving the rest of the cell as text", () => {
        const panel = createTablePanel(castTable());

        setFilter(panel, "li"); // Charlie and Alice (in both their name and id cells)
        expect(firstColumn(panel)).toEqual(["Charlie", "Alice"]);
        expect(marks(panel)).toEqual(["li", "li", "li", "li"]);

        const alice = [...panel.querySelectorAll("tbody tr")].find((tr) =>
            tr.textContent?.includes("Alice"),
        )!;
        const nameCell = alice.querySelector("td")!;
        expect(nameCell.textContent).toBe("Alice");
        expect(nameCell.querySelector("mark.table-mark")?.textContent).toBe("li");
    });

    it("adds no marks when the filter is empty", () => {
        const panel = createTablePanel(castTable());

        expect(marks(panel)).toEqual([]);
        setFilter(panel, "ali");
        setFilter(panel, "");
        expect(marks(panel)).toEqual([]);
    });

    it("labels the toggles and leaves them unpressed by default", () => {
        const panel = createTablePanel(castTable());
        const caseToggle = toggle(panel, "Match case");
        const wordToggle = toggle(panel, "Match whole word");

        expect(caseToggle.textContent).toBe("Aa");
        expect(wordToggle.textContent).toBe("ab|");
        expect(caseToggle.getAttribute("aria-pressed")).toBe("false");
        expect(wordToggle.getAttribute("aria-pressed")).toBe("false");
    });

    it("re-filters case-sensitively when Match case is pressed", () => {
        const panel = createTablePanel(castTable());

        setFilter(panel, "A"); // case-insensitive: Charlie, Alice (both have an 'a')
        expect(firstColumn(panel)).toEqual(["Charlie", "Alice"]);

        const caseToggle = toggle(panel, "Match case");
        caseToggle.click();
        expect(caseToggle.getAttribute("aria-pressed")).toBe("true");
        expect(firstColumn(panel)).toEqual(["Alice"]); // only Alice has an uppercase 'A'
        expect(marks(panel)).toEqual(["A"]);
    });

    it("re-filters to whole words when Match whole word is pressed", () => {
        const panel = createTablePanel(castTable());

        setFilter(panel, "Ali"); // substring of Alice / @alice
        expect(firstColumn(panel)).toEqual(["Alice"]);

        toggle(panel, "Match whole word").click();
        expect(panel.querySelector(".table-nomatch")).not.toBeNull(); // "Ali" is not a whole word
    });
});

describe("createTablePanel — copying a tag", () => {
    it("copies a tag capsule when it is clicked", () => {
        // Every table built here draws tag capsules, and a capsule wears a hover ring and carries
        // the text to copy — so the promise of a click belongs to the shared table, not to
        // whichever tab remembered to wire it.
        const writeText = vi.fn().mockResolvedValue(undefined);
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
        const table: SemanticTable = {
            title: "Speakers",
            columns: ["Name", "Tags"],
            emptyText: "No speakers.",
            rows: [
                {
                    cells: [
                        { text: "Guide" },
                        { text: "#wise", tags: [{ name: "wise", reserved: false }] },
                    ],
                },
            ],
        };

        const panel = createTablePanel(table);
        panel
            .querySelector<HTMLElement>(".dd-tag")
            ?.dispatchEvent(new MouseEvent("click", { bubbles: true }));

        expect(writeText).toHaveBeenCalledWith("#wise");
    });
});

describe("createTablePanel — copying an identifier", () => {
    function panelWith(cell: { text: string; copyable?: boolean }): HTMLElement {
        return createTablePanel({
            title: "Anchors",
            columns: ["Anchor"],
            emptyText: "No scenes.",
            rows: [{ cells: [cell] }],
        });
    }

    function clickCell(panel: HTMLElement): void {
        panel
            .querySelector<HTMLElement>("tbody td")
            ?.dispatchEvent(new MouseEvent("click", { bubbles: true }));
    }

    it("copies a cell a writer would paste into a script", () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });

        clickCell(panelWith({ text: "#the-market", copyable: true }));

        expect(writeText).toHaveBeenCalledExactlyOnceWith("#the-market");
    });

    it("leaves an ordinary cell alone, so clicking prose copies nothing", () => {
        // Only identifiers are copyable. A sentence-shaped cell — a jump's label, a scene title —
        // would copy something nobody asked for and steal the reader's selection.
        const writeText = vi.fn().mockResolvedValue(undefined);
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });

        clickCell(panelWith({ text: "Take the east road" }));

        expect(writeText).not.toHaveBeenCalled();
    });
});
