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
import type {
    PlaybookReport,
    PlaybookMetadataView,
    PlaybookSpeakerView,
    PlaybookAnchorView,
    SemanticTable,
} from "./model";
import { createTablePanel } from "./semantic-table";
import { initSplitDivider } from "./source-view";
import { copyToClipboard } from "./path-display";
import { initCollapsiblePanel } from "./collapse-toggle";
import { showToast } from "./toast";
import { compactSearch } from "./search-panel";
import { schemaHover } from "./playbook-schema";
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
                schemaHover(),
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

/**
 * The right pane: the playbook's header, speakers, and anchors, each its own collapsible panel.
 *
 * They are the Semantic tab's table panels — the same caret, search box, row count, and sortable
 * headers — because they answer the same kind of question about a different artifact, and a
 * reader who has learned one should not have to learn the other.
 */
function renderTables(playbook: PlaybookReport): HTMLElement {
    const wrapper = document.createElement("div");
    wrapper.className = "playbook-tables";
    for (const table of tablesOf(playbook)) {
        // Its own namespace: the Semantic tab has Speakers and Anchors panels too, and one
        // remembered key would make collapsing a panel here collapse that tab's as well.
        wrapper.appendChild(createTablePanel(table, "dd-playbook-panel-"));
    }
    wrapper.appendChild(schemaNote(playbook.metadata?.schemaUrl));
    wireClickToCopy(wrapper);
    return wrapper;
}

/** The three tables, in the order the format itself reads: what it is, who speaks, where jumps land. */
function tablesOf(playbook: PlaybookReport): SemanticTable[] {
    return [
        headerTable(playbook.metadata),
        speakerTable(playbook.speakers),
        anchorTable(playbook.anchors),
    ];
}

/** Copy the text of a clicked cell (any element carrying `data-copy`), and confirm it. */
function wireClickToCopy(root: HTMLElement): void {
    root.addEventListener("click", (event) => {
        const target = (event.target as Element | null)?.closest<HTMLElement>("[data-copy]");
        const value = target?.dataset.copy;
        if (!value) return;
        void copyToClipboard(value).then(() => showToast(`Copied ${value}`));
    });
}

/**
 * The playbook's header as a field/value table: what it was compiled from, what a host must
 * provide to run it, where it starts, and how big it is.
 */
function headerTable(metadata: PlaybookMetadataView | undefined): SemanticTable {
    const fields: [string, string][] =
        metadata == null
            ? []
            : [
                  ["Script", metadata.script],
                  ["Format version", String(metadata.formatVersion)],
                  ["Requires", listOrDash(metadata.requires)],
                  ["Uses", listOrDash(metadata.uses)],
                  ["Entry node", String(metadata.entry)],
                  ["Nodes", String(metadata.nodeCount)],
                  ["Anchors", String(metadata.anchorCount)],
              ];
    return {
        title: "Playbook",
        columns: ["Field", "Value"],
        rows: fields.map(([field, value]) => ({ cells: [{ text: field }, { text: value }] })),
        emptyText: "No playbook metadata yet.",
    };
}

/**
 * The playbook's speaker table: who can speak, the id a runtime looks them up by, which one owns
 * an unprefixed line, and the tags a host reads for portraits or voices.
 */
function speakerTable(speakers: readonly PlaybookSpeakerView[]): SemanticTable {
    return {
        title: "Speakers",
        columns: ["Name", "Id", "Default", "Tags"],
        rows: speakers.map((speaker) => ({
            cells: [
                // The anonymous speaker is the one an unprefixed line belongs to; it has no name.
                { text: speaker.name ?? "(anonymous)" },
                { text: speaker.id ?? "—" },
                { text: speaker.default ? "yes" : "—" },
                { text: speaker.tags.length === 0 ? "—" : speaker.tags.join(", ") },
            ],
        })),
        emptyText: "This playbook has no speakers.",
        // Which speaker owns an unprefixed line is the question worth filtering on.
        facetColumns: ["Default"],
    };
}

/** The anchors a jump may name, and the node each lands on. */
function anchorTable(anchors: readonly PlaybookAnchorView[]): SemanticTable {
    return {
        title: "Anchors",
        columns: ["Anchor", "Node"],
        rows: anchors.map((anchor) => ({
            // An anchor is written with its `#`, exactly as a jump names it.
            cells: [{ text: `#${anchor.name}` }, { text: String(anchor.node) }],
        })),
        emptyText: "No scene in this playbook can be jumped to by name.",
    };
}

/**
 * The published schema, linked below the tables. The playbook names it in its own `$schema`
 * field, but that is a URL in a document rather than something to click; this is the way to the
 * format's reference, and hovering a property in the editor shows what that reference says.
 */
function schemaNote(url: string | undefined): HTMLElement {
    const note = document.createElement("p");
    note.className = "playbook-schema-note";
    if (url == null) return note;
    const name = escapeHtml(url.slice(url.lastIndexOf("/") + 1));
    note.innerHTML =
        `Described by <a class="playbook-schema-link" href="${escapeHtml(url)}" target="_blank"` +
        ` rel="noopener noreferrer" title="${escapeHtml(url)}">${name}</a>.`;
    return note;
}

function listOrDash(values: readonly string[]): string {
    return values.length === 0 ? "—" : values.join(", ");
}
