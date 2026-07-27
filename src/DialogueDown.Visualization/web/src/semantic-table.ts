import type { SemanticTable, SemanticCell, SemanticRow } from "./model";
import { escapeHtml } from "./text";
import { colorOf } from "./palette";
import { initCollapsiblePanel } from "./collapse-toggle";
import {
    createTable,
    functionalUpdate,
    getCoreRowModel,
    getFilteredRowModel,
    getSortedRowModel,
    type ColumnDef,
    type TableState,
} from "@tanstack/table-core";

/**
 * Render one semantic table as a **collapsible panel**: a header bar (title + row count) that
 * toggles the table body to a bar and back, over an interactive table a writer can **filter** and
 * **sort** while its rows and cells keep the cross-link keys the entity highlighter reads. Sorting
 * and filtering are driven by TanStack `table-core` (headless), so this module still owns every
 * pixel — the category accents, cross-link attributes, and collapsible state all survive. The
 * collapsed state persists across reloads, reusing the report's collapsible-panel pattern.
 */
export function createTablePanel(table: SemanticTable): HTMLElement {
    const panel = document.createElement("section");
    panel.className = "table-panel";

    const count = document.createElement("span");
    count.className = "table-panel-count";
    count.textContent = String(table.rows.length);

    const header = document.createElement("button");
    header.type = "button";
    header.className = "table-panel-header";
    header.innerHTML =
        `<span class="table-panel-caret" aria-hidden="true"></span>` +
        `<span class="table-panel-title">${escapeHtml(table.title)}</span>`;
    header.appendChild(count);

    const body = document.createElement("div");
    body.className = "table-panel-body";
    body.appendChild(renderContent(table, (shown) => (count.textContent = String(shown))));

    panel.append(header, body);

    // Reuse the collapsible-panel state + persistence; the header bar is the toggle, so its
    // own button is unused. A per-title key remembers each panel independently across reloads.
    const collapsible = initCollapsiblePanel({
        container: panel,
        collapsedClass: "collapsed",
        storageKey: `dd-sem-panel-${slug(table.title)}`,
        name: table.title,
    });
    const reflect = (): void =>
        header.setAttribute("aria-expanded", String(!collapsible.isCollapsed()));
    header.addEventListener("click", () => {
        collapsible.toggle();
        reflect();
    });
    reflect();

    return panel;
}

/** The panel body: an interactive table, or a "none" note when the table has no rows. */
function renderContent(table: SemanticTable, setShown: (shown: number) => void): HTMLElement {
    if (table.rows.length === 0) {
        const empty = document.createElement("p");
        empty.className = "table-empty";
        empty.textContent = table.emptyText;
        return empty;
    }

    return buildInteractiveTable(table, setShown);
}

/**
 * A searchable, sortable table over `table.rows`. A `table-core` instance holds the sort and
 * global-filter state; on every change this re-renders the body from its row model and reports the
 * visible-row count to `setShown`, so the panel's count badge tracks the filter. The header cells
 * are built once (so keyboard focus survives a sort) and only their `aria-sort` updates.
 */
function buildInteractiveTable(
    table: SemanticTable,
    setShown: (shown: number) => void,
): HTMLElement {
    const container = document.createElement("div");
    container.className = "table-interactive";

    const search = document.createElement("input");
    search.type = "search";
    search.className = "table-search";
    search.placeholder = "Filter…";
    search.setAttribute("aria-label", `Filter ${table.title}`);

    const element = document.createElement("table");
    element.className = "semantic-table";
    element.dataset.table = slug(table.title);
    const headRow = document.createElement("tr");
    const thead = document.createElement("thead");
    thead.appendChild(headRow);
    const tbody = document.createElement("tbody");
    element.append(thead, tbody);

    // One column per source column; each reads its cell's text so sort and filter act on what the
    // reader sees. The accessor keeps rows as our own SemanticRow, so rendering is unchanged.
    const columns: ColumnDef<SemanticRow>[] = table.columns.map((name, index) => ({
        id: name,
        header: name,
        accessorFn: (row) => row.cells[index]?.text ?? "",
    }));

    let state: TableState;

    const instance = createTable<SemanticRow>({
        data: table.rows,
        columns,
        globalFilterFn: "includesString",
        enableSortingRemoval: true,
        state: {},
        onStateChange: (updater) => {
            state = functionalUpdate(updater, instance.getState());
            instance.setOptions((prev) => ({ ...prev, state }));
            render();
        },
        renderFallbackValue: null,
        getCoreRowModel: getCoreRowModel(),
        getSortedRowModel: getSortedRowModel(),
        getFilteredRowModel: getFilteredRowModel(),
    });

    // Seed the full default state (column pinning, order, sizing, …) so the headless header code
    // has its defaults; from here we only ever change `sorting` and `globalFilter` through it.
    state = instance.initialState;
    instance.setOptions((prev) => ({ ...prev, state }));

    // Header cells built once so a keyboard sort keeps focus; render() only refreshes aria-sort.
    const headerCells = instance.getFlatHeaders().map((header) => {
        const th = document.createElement("th");
        th.scope = "col";
        const button = document.createElement("button");
        button.type = "button";
        button.className = "th-sort";
        button.textContent = header.column.id;
        const toggle = header.column.getToggleSortingHandler();
        if (toggle) {
            button.addEventListener("click", (event) => toggle(event));
        }
        th.appendChild(button);
        return { th, id: header.column.id };
    });
    headRow.append(...headerCells.map((cell) => cell.th));

    function render(): void {
        for (const { th, id } of headerCells) {
            th.setAttribute("aria-sort", ariaSort(instance.getColumn(id)?.getIsSorted()));
        }

        const rows = instance.getRowModel().rows;
        if (rows.length === 0) {
            tbody.replaceChildren(noMatchRow(table.columns.length));
        } else {
            tbody.replaceChildren(...rows.map((row) => renderRow(row.original)));
        }
        setShown(rows.length);
    }

    search.addEventListener("input", () => instance.setGlobalFilter(search.value));

    render();
    container.append(search, element);
    return container;
}

/** The `aria-sort` value for a column's sort direction. */
function ariaSort(direction: false | "asc" | "desc" | undefined): string {
    if (direction === "asc") return "ascending";
    if (direction === "desc") return "descending";
    return "none";
}

/** The single "no matches" row shown when a filter hides every row. */
function noMatchRow(columnCount: number): HTMLElement {
    const tr = document.createElement("tr");
    const td = document.createElement("td");
    td.className = "table-nomatch";
    td.colSpan = columnCount;
    td.textContent = "No matches.";
    tr.appendChild(td);
    return tr;
}

/** A `<tr>` carrying the row's cross-link key and its cells. */
function renderRow(row: SemanticRow): HTMLElement {
    const tr = document.createElement("tr");
    if (row.entityKey) tr.setAttribute("data-entity-key", row.entityKey);
    for (const cell of row.cells) {
        tr.appendChild(renderCell(cell));
    }
    return tr;
}

/** A `<td>` carrying the cell's text, category color accent, and any cross-link key. */
function renderCell(cell: SemanticCell): HTMLElement {
    const td = document.createElement("td");
    td.textContent = cell.text;
    if (cell.entityKey) td.setAttribute("data-entity-key", cell.entityKey);
    if (cell.refKey) td.setAttribute("data-ref-key", cell.refKey);
    if (cell.category) {
        td.dataset.category = cell.category;
        td.style.setProperty("--cell-accent", colorOf(cell.category));
    }
    return td;
}

/** A panel/table slug from a title: lowercased, spaces to hyphens. */
function slug(title: string): string {
    return title.toLowerCase().replace(/\s+/g, "-");
}
