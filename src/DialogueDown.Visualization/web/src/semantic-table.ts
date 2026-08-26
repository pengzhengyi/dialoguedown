import tippy from "tippy.js";
import type { SemanticTable, SemanticCell, SemanticRow } from "./model";
import { escapeHtml } from "./text";
import { colorOf } from "./palette";
import { initCollapsiblePanel } from "./collapse-toggle";
import { findMatches, hasMatch, type MatchOptions, type MatchRange } from "./text-match";
import {
    columnFilteringFeature,
    constructTable,
    createFilteredRowModel,
    createSortedRowModel,
    filterFns,
    globalFilteringFeature,
    rowSortingFeature,
    tableFeatures,
    type ColumnDef,
    type FilterFn,
    type Table,
} from "@tanstack/table-core";
import { storeReactivityBindings } from "@tanstack/table-core/store-reactivity-bindings";
import { renderTags } from "./tag-chip";
import { wireClickToCopy } from "./copy-on-click";

// The feature set this table opts into. Since v9, `table-core` registers behavior explicitly
// rather than bundling every feature: sorting, per-column (facet) filtering, and the global
// search, each with its row model. Registering the stock `filterFns` keeps naming a filter by
// string ("equalsString") valid. Vanilla use must also supply the reactivity bindings, because
// no framework adapter is present to wire the table's state atoms into a render.
const features = tableFeatures({
    coreReactivityFeature: storeReactivityBindings(),
    columnFilteringFeature,
    globalFilteringFeature,
    rowSortingFeature,
    filteredRowModel: createFilteredRowModel(),
    sortedRowModel: createSortedRowModel(),
    filterFns,
});

type SemanticFeatures = typeof features;

/** The table's search: the typed query plus the Match Case / Match Whole Word toggle state. */
interface SearchQuery extends MatchOptions {
    query: string;
}

// The global filter: a row is kept when any of its cells contains the query under the current
// case/whole-word options. The highlight uses the same matcher, so the marks match what stays.
const globalMatch: FilterFn<SemanticFeatures, SemanticRow> = (row, columnId, value) => {
    const search = value as SearchQuery;
    return hasMatch(String(row.getValue(columnId) ?? ""), search.query, search);
};

// Lucide icons (ISC): a magnifier for the on-demand search, a funnel for a column's facet filter.
const SEARCH_ICON = svg('<circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/>');
const FILTER_ICON = svg('<polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"/>');

let facetGroupSeq = 0;

/**
 * Render one semantic table as a **collapsible panel** whose title bar carries the collapse caret,
 * an on-demand search toggle, and the (filter-aware) row count. Its rows sort on any column header,
 * filter through a revealed search box, and — for categorical columns — through a faceted popover
 * whose choice shows as a chip in the header. Sorting and filtering run on TanStack `table-core`
 * (headless), so this module still owns every pixel: category accents, cross-link keys, and the
 * collapsible state all survive. The collapsed state persists across reloads.
 *
 * `storagePrefix` namespaces that remembered state. Two tabs can hold same-named tables — the
 * Semantic tab and the Playbook tab both show Speakers and Anchors — and a shared key would make
 * collapsing one silently collapse the other.
 */
export function createTablePanel(
    table: SemanticTable,
    storagePrefix = "dd-sem-panel-",
): HTMLElement {
    const panel = document.createElement("section");
    panel.className = "table-panel";

    const toggle = document.createElement("button");
    toggle.type = "button";
    toggle.className = "table-panel-toggle";
    toggle.innerHTML =
        `<span class="table-panel-caret" aria-hidden="true"></span>` +
        `<span class="table-panel-title">${escapeHtml(table.title)}</span>`;

    const count = document.createElement("span");
    count.className = "table-panel-count";
    count.textContent = String(table.rows.length);

    const header = document.createElement("div");
    header.className = "table-panel-header";
    header.appendChild(toggle);

    // The search toggle only makes sense when there are rows to filter.
    const searchToggle = table.rows.length > 0 ? buildSearchToggle(table.title) : null;
    if (searchToggle) {
        header.appendChild(searchToggle);
    }
    header.appendChild(count);

    const body = document.createElement("div");
    body.className = "table-panel-body";
    body.appendChild(
        renderContent(table, (shown) => (count.textContent = String(shown)), searchToggle),
    );

    panel.append(header, body);

    // Collapse only via the caret/title toggle, so the search button never folds the panel.
    const collapsible = initCollapsiblePanel({
        container: panel,
        collapsedClass: "collapsed",
        storageKey: `${storagePrefix}${slug(table.title)}`,
        name: table.title,
    });
    const reflect = (): void =>
        toggle.setAttribute("aria-expanded", String(!collapsible.isCollapsed()));
    toggle.addEventListener("click", () => {
        collapsible.toggle();
        reflect();
    });
    reflect();

    wireClickToCopy(panel);
    return panel;
}

/** The magnifier button that reveals the search box; wired to the input in the body. */
function buildSearchToggle(title: string): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "table-panel-search";
    button.innerHTML = SEARCH_ICON;
    button.setAttribute("aria-label", `Search ${title}`);
    button.setAttribute("aria-expanded", "false");
    return button;
}

/** The panel body: an interactive table, or a "none" note when the table has no rows. */
function renderContent(
    table: SemanticTable,
    setShown: (shown: number) => void,
    searchToggle: HTMLButtonElement | null,
): HTMLElement {
    if (table.rows.length === 0) {
        const empty = document.createElement("p");
        empty.className = "table-empty";
        empty.textContent = table.emptyText;
        return empty;
    }

    return buildInteractiveTable(table, setShown, searchToggle);
}

/**
 * A sortable, filterable table over `table.rows`. A `table-core` instance holds the sort, global
 * filter, and per-column facet state; on every change this re-renders the body from its row model
 * and reports the visible-row count to `setShown`. Header cells are built once (so keyboard focus
 * survives a sort); only their `aria-sort` updates.
 */
function buildInteractiveTable(
    table: SemanticTable,
    setShown: (shown: number) => void,
    searchToggle: HTMLButtonElement | null,
): HTMLElement {
    const container = document.createElement("div");
    container.className = "table-interactive";

    const facetNames = new Set(table.facetColumns ?? []);

    const searchRow = document.createElement("div");
    searchRow.className = "table-search-row";
    searchRow.hidden = true;

    // A bordered field holding the input and the inline Match Case / Match Whole Word toggles,
    // matching the editor's search field.
    const field = document.createElement("div");
    field.className = "table-search-field";
    const search = document.createElement("input");
    search.type = "search";
    search.className = "table-search";
    search.placeholder = "Filter…";
    search.setAttribute("aria-label", `Filter ${table.title}`);
    const caseToggle = matchToggle("Aa", "Match case");
    const wordToggle = matchToggle("ab|", "Match whole word");
    const toggles = document.createElement("div");
    toggles.className = "table-search-toggles";
    toggles.append(caseToggle, wordToggle);
    field.append(search, toggles);
    searchRow.appendChild(field);

    const element = document.createElement("table");
    element.className = "semantic-table";
    element.dataset.table = slug(table.title);
    const headRow = document.createElement("tr");
    const thead = document.createElement("thead");
    thead.appendChild(headRow);
    const tbody = document.createElement("tbody");
    element.append(thead, tbody);

    // One column per source column; each reads its cell's text so sort and filter act on what the
    // reader sees. A categorical column also matches by exact value, for its faceted filter.
    const columns: ColumnDef<SemanticFeatures, SemanticRow>[] = table.columns.map(
        (name, index) => ({
            id: name,
            header: name,
            accessorFn: (row) => row.cells[index]?.text ?? "",
            filterFn: facetNames.has(name) ? "equalsString" : "includesString",
        }),
    );

    const instance = constructTable({
        features,
        data: table.rows,
        columns,
        globalFilterFn: globalMatch,
        enableSortingRemoval: true,
        renderFallbackValue: null,
    });

    // v9 tables own their state, so a change is observed rather than fed back in: every sort or
    // filter update publishes to the store, and re-rendering from that keeps the body in step.
    instance.store.subscribe(() => render());

    // Header cells built once so a keyboard sort keeps focus; render() only refreshes aria-sort.
    const headerCells = instance.getFlatHeaders().map((headerColumn) => {
        const th = document.createElement("th");
        th.scope = "col";
        const inner = document.createElement("div");
        inner.className = "th-inner";

        const sort = document.createElement("button");
        sort.type = "button";
        sort.className = "th-sort";
        sort.textContent = headerColumn.column.id;
        const toggleSort = headerColumn.column.getToggleSortingHandler();
        if (toggleSort) {
            sort.addEventListener("click", (event) => toggleSort(event));
        }
        inner.appendChild(sort);

        if (facetNames.has(headerColumn.column.id)) {
            const facet = buildFacetControl(table, headerColumn.column.id, instance);
            if (facet) {
                inner.appendChild(facet);
            }
        }

        th.appendChild(inner);
        return { th, id: headerColumn.column.id };
    });
    headRow.append(...headerCells.map((cell) => cell.th));

    function render(): void {
        for (const { th, id } of headerCells) {
            th.setAttribute("aria-sort", ariaSort(instance.getColumn(id)?.getIsSorted()));
        }

        const query = instance.store.state.globalFilter as SearchQuery | undefined;
        const rows = instance.getRowModel().rows;
        if (rows.length === 0) {
            tbody.replaceChildren(noMatchRow(table.columns.length));
        } else {
            tbody.replaceChildren(...rows.map((row) => renderRow(row.original, query)));
        }
        setShown(rows.length);
    }

    let caseSensitive = false;
    let wholeWord = false;

    const applySearch = (): void => {
        const query = search.value;
        instance.setGlobalFilter(
            query.length === 0 ? undefined : { query, caseSensitive, wholeWord },
        );
        searchToggle?.classList.toggle("active", query.length > 0);
    };

    const wireToggle = (toggle: HTMLButtonElement, set: (pressed: boolean) => void): void => {
        toggle.addEventListener("click", () => {
            const pressed = toggle.getAttribute("aria-pressed") !== "true";
            toggle.setAttribute("aria-pressed", String(pressed));
            set(pressed);
            applySearch();
            search.focus();
        });
    };
    wireToggle(caseToggle, (pressed) => (caseSensitive = pressed));
    wireToggle(wordToggle, (pressed) => (wholeWord = pressed));

    if (searchToggle) {
        searchToggle.addEventListener("click", () => {
            const open = searchRow.hidden;
            searchRow.hidden = !open;
            searchToggle.setAttribute("aria-expanded", String(open));
            if (open) {
                search.focus();
            }
        });
    }
    search.addEventListener("input", applySearch);

    render();
    container.append(searchRow, element);
    return container;
}

/** A Match Case / Match Whole Word toggle button, styled like the editor's search toggles. */
function matchToggle(label: string, title: string): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "dd-search-toggle";
    button.textContent = label;
    button.title = title;
    button.setAttribute("aria-label", title);
    button.setAttribute("aria-pressed", "false");
    return button;
}

/**
 * The facet control for a categorical column: a funnel button that, when a value is chosen, becomes
 * a value chip. Clicking it opens a radio popover (All plus the column's distinct values); choosing
 * one applies that column's exact-match filter and All clears it.
 */
function buildFacetControl(
    table: SemanticTable,
    columnName: string,
    instance: Table<SemanticFeatures, SemanticRow>,
): HTMLElement | null {
    const index = table.columns.indexOf(columnName);
    const values = distinctValues(table.rows, index);
    if (values.length === 0) {
        return null;
    }
    const control = document.createElement("button");
    control.type = "button";
    control.className = "th-facet";
    control.setAttribute("aria-haspopup", "true");

    const showInactive = (): void => {
        control.classList.remove("active");
        control.innerHTML = FILTER_ICON;
        control.setAttribute("aria-label", `Filter by ${columnName}`);
    };
    const showValue = (value: string): void => {
        control.classList.add("active");
        control.textContent = "";
        const chip = document.createElement("span");
        chip.className = "th-facet-value";
        chip.textContent = value;
        control.appendChild(chip);
        control.setAttribute("aria-label", `${columnName}: ${value}. Change filter`);
    };
    showInactive();

    const groupName = `facet-${slug(columnName)}-${facetGroupSeq++}`;
    const popover = document.createElement("div");
    popover.className = "facet-popover";
    popover.setAttribute("role", "radiogroup");
    popover.setAttribute("aria-label", `Filter by ${columnName}`);

    function apply(value: string): void {
        instance.getColumn(columnName)?.setFilterValue(value || undefined);
        if (value) {
            showValue(value);
        } else {
            showInactive();
        }
        tip.hide();
    }

    const radios = ["", ...values].map((value) => {
        const option = document.createElement("label");
        option.className = "facet-option";
        const input = document.createElement("input");
        input.type = "radio";
        input.name = groupName;
        input.value = value;
        input.checked = value === "";
        input.addEventListener("change", () => {
            if (input.checked) {
                apply(value);
            }
        });
        const text = document.createElement("span");
        text.textContent = value === "" ? "All" : value;
        option.append(input, text);
        return option;
    });
    popover.append(...radios);

    const tip = tippy(control, {
        content: popover,
        interactive: true,
        trigger: "click",
        placement: "bottom-end",
        appendTo: () => document.body,
        arrow: false,
        offset: [0, 4],
        theme: "dd-facet",
    });

    return control;
}

/** The `aria-sort` value for a column's sort direction. */
function ariaSort(direction: false | "asc" | "desc" | undefined): string {
    if (direction === "asc") return "ascending";
    if (direction === "desc") return "descending";
    return "none";
}

/** The distinct non-empty cell texts of one column, in first-seen order. */
function distinctValues(rows: readonly SemanticRow[], index: number): string[] {
    const seen = new Set<string>();
    const values: string[] = [];
    for (const row of rows) {
        const text = row.cells[index]?.text ?? "";
        if (text !== "" && !seen.has(text)) {
            seen.add(text);
            values.push(text);
        }
    }
    return values;
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
function renderRow(row: SemanticRow, query: SearchQuery | undefined): HTMLElement {
    const tr = document.createElement("tr");
    if (row.entityKey) tr.setAttribute("data-entity-key", row.entityKey);
    for (const cell of row.cells) {
        tr.appendChild(renderCell(cell, query));
    }
    return tr;
}

/** A `<td>` carrying the cell's text, category color accent, and any cross-link key. */
function renderCell(cell: SemanticCell, query: SearchQuery | undefined): HTMLElement {
    const td = document.createElement("td");
    // An identifier is something a writer lifts into a script, so it offers itself for copying.
    // `data-copy` is all the shared listener needs; the class carries the hover cue.
    if (cell.copyable && cell.text !== "") {
        td.dataset.copy = cell.text;
        td.classList.add("dd-copy");
        td.title = "Click to copy";
    }
    if (cell.entityKey) td.setAttribute("data-entity-key", cell.entityKey);
    if (cell.refKey) td.setAttribute("data-ref-key", cell.refKey);
    if (cell.category) {
        td.dataset.category = cell.category;
        td.style.setProperty("--cell-accent", colorOf(cell.category));
    }
    // A tag cell is drawn as capsules. Its `text` stays the plain rendering, so search and sort
    // still read the cell; only the drawing differs.
    if (cell.tags) {
        if (cell.tags.length > 0) td.appendChild(renderTags(cell.tags));
        return td;
    }

    const ranges = query ? findMatches(cell.text, query.query, query) : [];
    if (ranges.length === 0) {
        td.textContent = cell.text;
    } else {
        highlightInto(td, cell.text, ranges);
    }
    return td;
}

/** Fills `td` with `text`, wrapping each match range in a `<mark>` so it stands out. */
function highlightInto(td: HTMLElement, text: string, ranges: MatchRange[]): void {
    let cursor = 0;
    for (const range of ranges) {
        if (range.start > cursor) {
            td.appendChild(document.createTextNode(text.slice(cursor, range.start)));
        }
        const mark = document.createElement("mark");
        mark.className = "table-mark";
        mark.textContent = text.slice(range.start, range.end);
        td.appendChild(mark);
        cursor = range.end;
    }
    if (cursor < text.length) {
        td.appendChild(document.createTextNode(text.slice(cursor)));
    }
}

/** A 15px stroked Lucide-style icon from its inner paths. */
function svg(paths: string): string {
    return (
        `<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" ` +
        `stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">` +
        `${paths}</svg>`
    );
}

/** A panel/table slug from a title: lowercased, spaces to hyphens. */
function slug(title: string): string {
    return title.toLowerCase().replace(/\s+/g, "-");
}
