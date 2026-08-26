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
    syntaxHighlighting,
    HighlightStyle,
    foldGutter,
    codeFolding,
    foldKeymap,
    bracketMatching,
} from "@codemirror/language";
import { searchKeymap, highlightSelectionMatches } from "@codemirror/search";
import { json } from "@codemirror/lang-json";
import { tags } from "@lezer/highlight";
import { foldGutterMarker } from "./fold-glyph";
import type {
    PlaybookReport,
    PlaybookMetadataView,
    PlaybookSpeakerView,
    PlaybookAnchorView,
    SemanticTable,
    SemanticCell,
} from "./model";
import { createTablePanel } from "./semantic-table";
import { initSplitDivider } from "./source-view";
import { initCollapsiblePanel } from "./collapse-toggle";
import { compactSearch } from "./search-panel";
import { gotoLineKeymap } from "./goto-line";
import { schemaHover } from "./playbook-schema";
import { escapeHtml } from "./text";
import { tagLabel } from "./tag-chip";
import { lineOf, revealLine, type PlaybookTarget } from "./playbook-jump";

/**
 * JSON highlighting driven by CSS variables, so the playbook follows the page's light/dark theme
 * live like the Markdown and TOML editors.
 *
 * The roles are VS Code's — a key, a string, a number, and a literal each on their own hue —
 * because a playbook is nearly all quoted strings and shades of one blue would not separate
 * them. The Lezer grammar is what makes it possible at all: the legacy tokenizer this replaced
 * emitted a single token for a property name and a string value alike.
 *
 * Punctuation is the exception, muted rather than VS Code's plain black. This editor is read,
 * not written, so the braces that give a block its shape should recede behind the data.
 */
const jsonHighlightStyle = HighlightStyle.define([
    { tag: [tags.propertyName, tags.definition(tags.propertyName)], color: "var(--json-key)" },
    { tag: tags.string, color: "var(--json-string)" },
    { tag: tags.number, color: "var(--json-number)" },
    { tag: [tags.bool, tags.null, tags.atom], color: "var(--json-literal)" },
    { tag: [tags.separator, tags.squareBracket, tags.brace], color: "var(--md-muted)" },
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

    const editor = playbook.json != null ? mountEditor(pane, playbook.json) : null;
    if (editor === null) pane.appendChild(renderUnavailable(playbook.unavailable));
    side.appendChild(renderTables(playbook, editor));

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
                json(),
                syntaxHighlighting(jsonHighlightStyle),
                EditorView.lineWrapping,
                keymap.of([...defaultKeymap, ...gotoLineKeymap, ...searchKeymap, ...foldKeymap]),
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
function renderTables(playbook: PlaybookReport, editor: EditorView | null): HTMLElement {
    const wrapper = document.createElement("div");
    wrapper.className = "playbook-tables";
    if (editor !== null) wireJumps(wrapper, editor);
    for (const table of tablesOf(playbook)) {
        // Its own namespace: the Semantic tab has Speakers and Anchors panels too, and one
        // remembered key would make collapsing a panel here collapse that tab's as well.
        wrapper.appendChild(createTablePanel(table, "dd-playbook-panel-"));
    }
    wrapper.appendChild(schemaNote(playbook.metadata?.schemaUrl));
    return wrapper;
}

/**
 * Take the reader to the place a clicked cell stands for.
 *
 * Delegated from the tables, so a panel that re-renders its rows on a search or a sort keeps
 * working. A target that no longer resolves is left alone rather than guessed at: the reader
 * stays where they are instead of being sent somewhere plausible and wrong.
 */
function wireJumps(root: HTMLElement, editor: EditorView): void {
    root.addEventListener("click", (event) => {
        const cell = (event.target as Element | null)?.closest<HTMLElement>("[data-jump]");
        if (!cell?.dataset.jump) return;
        const line = lineOf(editor.state, JSON.parse(cell.dataset.jump) as PlaybookTarget);
        if (line !== null) revealLine(editor, line);
    });
}

/** The three tables, in the order the format itself reads: what it is, who speaks, where jumps land. */
function tablesOf(playbook: PlaybookReport): SemanticTable[] {
    return [
        headerTable(playbook.metadata),
        speakerTable(playbook.speakers),
        anchorTable(playbook.anchors),
    ];
}

/**
 * The playbook's header as a field/value table: what it was compiled from, what a host must
 * provide to run it, where it starts, and how big it is.
 */
function headerTable(metadata: PlaybookMetadataView | undefined): SemanticTable {
    const fields: [string, SemanticCell][] =
        metadata == null
            ? []
            : [
                  ["Script", { text: metadata.script }],
                  ["Format version", { text: String(metadata.formatVersion) }],
                  ["Requires", { text: metadata.requires.join(", ") }],
                  ["Uses", { text: metadata.uses.join(", ") }],
                  // Where a playthrough begins is a node like any other, so it goes there too.
                  [
                      "Entry node",
                      { text: String(metadata.entry), jump: { kind: "node", id: metadata.entry } },
                  ],
                  ["Nodes", { text: String(metadata.nodeCount) }],
                  ["Anchors", { text: String(metadata.anchorCount) }],
              ];
    return {
        title: "Playbook",
        columns: ["Field", "Value"],
        rows: fields.map(([field, value]) => ({ cells: [{ text: field }, value] })),
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
        rows: speakers.map((speaker, index) => ({
            cells: [
                // The anonymous speaker is the one an unprefixed line belongs to. Its
                // namelessness is a fact about the script, not a gap in the table, so it is the
                // one absence worth naming.
                // Bound by index here, not read off the row: a sorted table no longer has the
                // speaker in the position the array gave it.
                { text: speaker.name ?? "(anonymous)", jump: { kind: "speaker", index } },
                // Everything else says nothing when there is nothing to say, so the eye lands on
                // the speakers that do carry an id, a tag, or the default mark.
                // Written with its `@`, exactly as a script references it and as the other two
                // tabs show it — and copyable, so a writer can lift it straight into a line.
                { text: speaker.id == null ? "" : `@${speaker.id}`, copyable: true },
                { text: speaker.default ? "✓" : "" },
                { text: speaker.tags.map(tagLabel).join(" "), tags: speaker.tags },
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
            // An anchor is written with its `#`, exactly as a jump names it; the node it lands
            // on takes the reader to that node in the JSON beside it.
            cells: [
                { text: `#${anchor.name}`, copyable: true },
                { text: String(anchor.node), jump: { kind: "node", id: anchor.node } },
            ],
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
