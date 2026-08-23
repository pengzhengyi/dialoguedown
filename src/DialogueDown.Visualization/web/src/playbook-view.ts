import { EditorState } from "@codemirror/state";
import {
    EditorView,
    lineNumbers,
    keymap,
    drawSelection,
    highlightActiveLine,
    highlightActiveLineGutter,
} from "@codemirror/view";
import { defaultKeymap } from "@codemirror/commands";
import {
    StreamLanguage,
    syntaxHighlighting,
    HighlightStyle,
    foldGutter,
    codeFolding,
    foldKeymap,
    bracketMatching,
} from "@codemirror/language";
import { searchKeymap, highlightSelectionMatches } from "@codemirror/search";
import { json } from "@codemirror/legacy-modes/mode/javascript";
import { tags } from "@lezer/highlight";
import { foldGutterMarker } from "./fold-glyph";
import type { PlaybookReport, PlaybookMetadataView, PlaybookSpeakerView } from "./model";
import { initSplitDivider } from "./source-view";
import { copyToClipboard } from "./path-display";
import { initCollapsiblePanel } from "./collapse-toggle";
import { showToast } from "./toast";
import { compactSearch } from "./search-panel";
import { escapeHtml } from "./text";

/**
 * JSON highlighting driven by the same CSS variables the Markdown and TOML editors use, so the
 * playbook follows the page's light/dark theme live.
 */
const jsonHighlightStyle = HighlightStyle.define([
    { tag: [tags.propertyName, tags.definition(tags.propertyName)], color: "var(--md-heading)" },
    { tag: tags.string, color: "var(--md-code)" },
    { tag: [tags.number, tags.bool, tags.atom, tags.null], color: "var(--md-link)" },
    { tag: [tags.bracket, tags.squareBracket, tags.brace], color: "var(--md-muted)" },
]);

/**
 * The Playbook tab: the compiled playbook shown as a two-column split — the serialized JSON a
 * host would load (read-only, JSON-highlighted) on the left and the tables that summarize it on
 * the right — reusing the Source tab's split machinery. The playbook is the runtime's artifact,
 * so unlike the Source and Config editors it is never editable: it is compiled, not authored.
 *
 * A recompile rebuilds the whole tab rather than patching it: the tab sits after the graph
 * stages, which are replaced wholesale, and the split and collapse choices it would otherwise
 * preserve are already remembered across reloads.
 */
export function createPlaybookView(playbook: PlaybookReport): HTMLElement {
    const container = document.createElement("div");
    container.className = "playbook-view";

    const pane = document.createElement("div");
    pane.className = "playbook-source";

    const divider = document.createElement("div");
    divider.className = "playbook-divider";

    const side = document.createElement("div");
    side.className = "playbook-side";

    if (playbook.json != null) mountEditor(pane, playbook.json);
    else pane.appendChild(renderUnavailable(playbook.unavailable));
    side.appendChild(renderTables(playbook));

    container.append(pane, divider, side);
    initSplitDivider(container, divider, "--playbook-split", "playbook-collapsed");

    // The right (tables) panel can be hidden to give the JSON the full width, the same way the
    // Config tab hides its speakers. The toggle lives on the divider and doubles as the
    // always-present re-open handle; the choice is remembered across reloads.
    const tablesPanel = initCollapsiblePanel({
        container,
        collapsedClass: "playbook-collapsed",
        storageKey: "dd-playbook-collapsed",
        name: "playbook tables",
    });
    divider.appendChild(tablesPanel.button);

    return container;
}

/** A focusable, read-only CodeMirror over the serialized playbook. */
function mountEditor(parent: HTMLElement, source: string): EditorView {
    return new EditorView({
        parent,
        state: EditorState.create({
            doc: source,
            extensions: [
                lineNumbers(),
                highlightActiveLineGutter(),
                foldGutter({ markerDOM: foldGutterMarker }),
                codeFolding(),
                drawSelection(),
                highlightActiveLine(),
                highlightSelectionMatches(),
                bracketMatching(),
                compactSearch(),
                EditorState.readOnly.of(true),
                EditorView.contentAttributes.of({
                    "aria-label": "Compiled playbook",
                    "aria-readonly": "true",
                    tabindex: "0",
                }),
                StreamLanguage.define(json),
                syntaxHighlighting(jsonHighlightStyle),
                EditorView.lineWrapping,
                keymap.of([...defaultKeymap, ...searchKeymap, ...foldKeymap]),
            ],
        }),
    });
}

/** The left pane when the compile never reached a playbook — the same news the graph tab gives. */
function renderUnavailable(reason: string | undefined): HTMLElement {
    const note = document.createElement("div");
    note.className = "playbook-empty-state";
    note.innerHTML =
        `<p>${escapeHtml(reason ?? "No playbook was produced.")}</p>` +
        `<p>Fix the errors reported in the Source tab and the playbook appears here.</p>`;
    return note;
}

/** The right pane: the playbook's header and its speakers, stacked. */
function renderTables(playbook: PlaybookReport): HTMLElement {
    const wrapper = document.createElement("div");
    wrapper.className = "playbook-tables";
    wrapper.append(renderMetadata(playbook.metadata), renderSpeakers(playbook.speakers));
    wireClickToCopy(wrapper);
    return wrapper;
}

/** Copy the text of a clicked cell or tag chip (any element carrying `data-copy`), and confirm it. */
function wireClickToCopy(root: HTMLElement): void {
    root.addEventListener("click", (event) => {
        const target = (event.target as Element | null)?.closest<HTMLElement>("[data-copy]");
        const value = target?.dataset.copy;
        if (!value) return;
        void copyToClipboard(value).then(() => showToast(`Copied ${value}`));
    });
}

/**
 * The playbook's header as a label/value table: what it was compiled from, what a host must
 * provide to run it, where it starts, and how big it is.
 */
function renderMetadata(metadata: PlaybookMetadataView | undefined): HTMLElement {
    const wrapper = document.createElement("div");
    wrapper.className = "playbook-metadata";
    wrapper.innerHTML = `<h2 class="playbook-heading">Playbook</h2>`;
    if (!metadata) {
        wrapper.appendChild(renderEmptyNote("No playbook metadata yet."));
        return wrapper;
    }

    const rows: [string, string][] = [
        ["Script", metadata.script],
        ["Format version", String(metadata.formatVersion)],
        ["Requires", listOrDash(metadata.requires)],
        ["Uses", listOrDash(metadata.uses)],
        ["Entry node", String(metadata.entry)],
        ["Nodes", String(metadata.nodeCount)],
        ["Anchors", String(metadata.anchorCount)],
    ];
    const table = document.createElement("table");
    table.className = "semantic-table playbook-metadata-table";
    table.innerHTML =
        `<tbody>` +
        rows
            .map(
                ([label, value]) =>
                    `<tr><th scope="row">${escapeHtml(label)}</th>${cell(value)}</tr>`,
            )
            .join("") +
        `</tbody>`;
    wrapper.appendChild(table);
    return wrapper;
}

/**
 * The playbook's speaker table: who can speak, the id a runtime looks them up by, which one owns
 * an unprefixed line, and the tags a host reads for portraits or voices. Every id, name, and tag
 * is click-to-copy, so a value can be lifted straight into host code.
 */
function renderSpeakers(speakers: PlaybookSpeakerView[]): HTMLElement {
    const wrapper = document.createElement("div");
    wrapper.className = "playbook-speakers";
    wrapper.innerHTML = `<h2 class="playbook-heading">Speakers</h2>`;
    if (speakers.length === 0) {
        wrapper.appendChild(renderEmptyNote("This playbook has no speakers."));
        return wrapper;
    }

    const rows = speakers.map((speaker) => `<tr>${speakerCells(speaker)}</tr>`).join("");
    const table = document.createElement("table");
    table.className = "semantic-table playbook-speakers-table";
    table.innerHTML =
        `<thead><tr>` +
        `<th scope="col">Name</th><th scope="col">Id</th>` +
        `<th scope="col">Default</th><th scope="col">Tags</th>` +
        `</tr></thead><tbody>${rows}</tbody>`;
    wrapper.appendChild(table);
    return wrapper;
}

function speakerCells(speaker: PlaybookSpeakerView): string {
    // The anonymous default speaker has no name — it is the one a line with no prefix belongs to.
    const name =
        speaker.name == null
            ? `<td><span class="playbook-anonymous">(anonymous)</span></td>`
            : copyCell(speaker.name);
    const id =
        speaker.id == null
            ? `<td><span class="playbook-empty">—</span></td>`
            : copyCell(speaker.id);
    const isDefault = speaker.default
        ? `<td><span class="playbook-default">yes</span></td>`
        : `<td><span class="playbook-empty">—</span></td>`;
    const tags =
        speaker.tags.length === 0
            ? `<td><span class="playbook-empty">—</span></td>`
            : `<td><div class="playbook-tags">${speaker.tags.map(tagChip).join(" ")}</div></td>`;
    return name + id + isDefault + tags;
}

/** One tag chip, click-to-copy. */
function tagChip(tag: string): string {
    const safe = escapeHtml(tag);
    return `<span class="playbook-tag" data-copy="${safe}" title="Click to copy">${safe}</span>`;
}

/** A value cell whose displayed text is exactly what a click copies. */
function copyCell(text: string): string {
    const safe = escapeHtml(text);
    return `<td class="playbook-copy" data-copy="${safe}" title="Click to copy">${safe}</td>`;
}

function cell(text: string): string {
    return `<td>${escapeHtml(text)}</td>`;
}

function listOrDash(values: readonly string[]): string {
    return values.length === 0 ? "—" : values.join(", ");
}

function renderEmptyNote(text: string): HTMLElement {
    const note = document.createElement("p");
    note.className = "playbook-empty";
    note.textContent = text;
    return note;
}
