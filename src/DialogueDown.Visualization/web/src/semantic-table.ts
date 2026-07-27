import type { SemanticTable, SemanticCell } from "./model";
import { escapeHtml } from "./text";
import { colorOf } from "./palette";
import { initCollapsiblePanel } from "./collapse-toggle";

/**
 * Render one semantic table as a **collapsible panel**: a header bar (title + row count) that
 * toggles the table body to a bar and back, over a table whose rows and cells carry the
 * cross-link keys the entity highlighter reads. The collapsed state persists across reloads,
 * reusing the report's collapsible-panel pattern.
 */
export function createTablePanel(table: SemanticTable): HTMLElement {
    const panel = document.createElement("section");
    panel.className = "table-panel";

    const header = document.createElement("button");
    header.type = "button";
    header.className = "table-panel-header";
    header.innerHTML =
        `<span class="table-panel-caret" aria-hidden="true"></span>` +
        `<span class="table-panel-title">${escapeHtml(table.title)}</span>` +
        `<span class="table-panel-count">${table.rows.length}</span>`;

    const body = document.createElement("div");
    body.className = "table-panel-body";
    body.appendChild(renderTable(table));

    panel.append(header, body);

    // Reuse the collapsible-panel state + persistence; the header bar is the toggle, so its
    // own button is unused. A per-title key remembers each panel independently across reloads.
    const collapsible = initCollapsiblePanel({
        container: panel,
        collapsedClass: "collapsed",
        storageKey: `dd-sem-panel-${table.title.toLowerCase().replace(/\s+/g, "-")}`,
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

/** The `<table>` for a semantic table, or a "none" note when it has no rows. */
function renderTable(table: SemanticTable): HTMLElement {
    if (table.rows.length === 0) {
        const empty = document.createElement("p");
        empty.className = "table-empty";
        empty.textContent = table.emptyText;
        return empty;
    }

    const element = document.createElement("table");
    element.className = "semantic-table";
    element.dataset.table = table.title.toLowerCase().replace(/\s+/g, "-");

    const head = document.createElement("thead");
    const headRow = document.createElement("tr");
    for (const column of table.columns) {
        const th = document.createElement("th");
        th.scope = "col";
        th.textContent = column;
        headRow.appendChild(th);
    }
    head.appendChild(headRow);

    const bodyEl = document.createElement("tbody");
    for (const row of table.rows) {
        const tr = document.createElement("tr");
        if (row.entityKey) tr.setAttribute("data-entity-key", row.entityKey);
        for (const cell of row.cells) {
            tr.appendChild(renderCell(cell));
        }
        bodyEl.appendChild(tr);
    }

    element.append(head, bodyEl);
    return element;
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
