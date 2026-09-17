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
    PlaybookNodeView,
    PlaybookSegmentView,
    SummaryRole,
    SemanticTable,
    SemanticCell,
    SemanticList,
    SemanticSegment,
} from "./model";
import { createEntityHighlighter } from "./entity-highlight";
import { createTablePanel } from "./semantic-table";
import { initPieceTooltips } from "./tooltips";
import { initSplitDivider } from "./source-view";
import { initCollapsiblePanel } from "./collapse-toggle";
import { compactSearch } from "./search-panel";
import { gotoLineKeymap } from "./goto-line";
import { schemaHover } from "./playbook-schema";
import { edgeTooltipHtml, escapeHtml } from "./text";
import { tagLabel } from "./tag-chip";
import { lineOf, revealLine, type PlaybookTarget } from "./playbook-jump";
import { playbookReferences, playbookReferenceKeymap } from "./playbook-references";

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

    // Hovering a node's row or any way out that names it relates the two: the row carries the
    // node's entity key and each way out references it, so the highlighter lights up whichever
    // else is on screen.
    createEntityHighlighter(container);

    // The pieces of a summary say what they are on hover: a query, a command, a jump's words.
    initPieceTooltips(container);

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
                playbookReferences(),
                EditorState.readOnly.of(true),
                EditorView.contentAttributes.of({
                    "aria-label": "Compiled playbook",
                    "aria-readonly": "true",
                    tabindex: "0",
                }),
                json(),
                syntaxHighlighting(jsonHighlightStyle),
                EditorView.lineWrapping,
                keymap.of([
                    ...playbookReferenceKeymap,
                    ...defaultKeymap,
                    ...gotoLineKeymap,
                    ...searchKeymap,
                    ...foldKeymap,
                ]),
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

/**
 * The four tables, in the order the format itself reads: what it is, who speaks, where jumps
 * land, and the nodes themselves.
 */
function tablesOf(playbook: PlaybookReport): SemanticTable[] {
    return [
        headerTable(playbook.metadata),
        speakerTable(playbook.speakers),
        anchorTable(playbook.anchors),
        nodeTable(playbook.nodes),
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
 * The playbook's speaker table: who can speak, the id a runtime looks them up by, the tags a host
 * reads for portraits or voices, and which one owns an unprefixed line.
 *
 * The columns are in the Semantic Model tab's order, so a reader who has learned one table reads
 * the other the same way.
 */
function speakerTable(speakers: readonly PlaybookSpeakerView[]): SemanticTable {
    return {
        title: "Speakers",
        columns: ["Name", "@id", "Tags", "Default"],
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
                { text: speaker.tags.map(tagLabel).join(" "), tags: speaker.tags },
                { text: speaker.default ? "✓" : "" },
            ],
        })),
        emptyText: "This playbook has no speakers.",
        // Which speaker owns an unprefixed line, and which carry a given tag, are the questions
        // worth filtering on.
        facetColumns: ["Default", "Tags"],
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
 * The class each summary role wears in the Nodes table. A writer's own words (`plain`) carry
 * none, so they keep the cell's own colour while the report's scaffolding steps back; so do the
 * roles whose meaning is drawn rather than painted — a boundary is a break and a target is a link.
 */
const SUMMARY_SEGMENT_CLASS: Record<SummaryRole, string | undefined> = {
    speaker: "dd-sum-speaker",
    keyword: "dd-sum-keyword",
    separator: "dd-sum-separator",
    boundary: undefined,
    command: "dd-sum-command",
    query: "dd-sum-query",
    absent: "dd-sum-absent",
    target: undefined,
    plain: undefined,
};

/** What a piece means, in the shape the Dialogue Graph explains its routes in. */
interface PieceTip {
    /** What the piece is called, in the graph's vocabulary. */
    name: string;
    /** What it does, in one sentence. */
    meaning: string;
}

/**
 * What a role means, for the roles whose own words do not say — `{Gold}` is a query only to a
 * reader who has learned the script language, and a `<no speech>` is a stand-in only to a reader
 * who has met one. Grammar and a writer's words explain themselves, so they carry none.
 */
const SUMMARY_SEGMENT_TIP: Partial<Record<SummaryRole, PieceTip>> = {
    speaker: {
        name: "Speaker",
        meaning: "Who says this line. A stand-in in brackets is the playbook's own, not a name.",
    },
    command: { name: "Command", meaning: "Performed by the host when this node is reached." },
    query: {
        name: "Query",
        meaning:
            "Answered while the game runs: the playbook carries the question, not the answer. " +
            "A trailing `?` asks for true or false.",
    },
    absent: {
        name: "Stand-in",
        meaning: "The report's own word, written where the script left nothing.",
    },
};

/**
 * What a piece that names a node means, named for the way out it stands for. The kind decides,
 * because the same shape — a piece carrying a target — is a conditional arm in one node, a jump in
 * another, and the pick a menu offers in a third, and the reader is owed the word the graph would
 * use for it.
 */
const TARGET_TIP: Readonly<Record<string, PieceTip>> = {
    branch: {
        name: "Conditional",
        meaning: "This arm's target: taken only while its condition holds.",
    },
    control: {
        name: "Jump",
        meaning: "A divert (⇒): control leaves the written order and resumes at the target.",
    },
    choice: {
        name: "Choice",
        meaning: "One arm of a menu: taken when the player picks it.",
    },
    "random-choice": {
        name: "Random choice",
        meaning: "One arm of a draw: taken when its odds come up.",
    },
};

const TARGET_TIP_FALLBACK: PieceTip = {
    name: "Target",
    meaning: "A node control can reach from here.",
};

/** What a piece that can be followed adds to its own meaning. */
const TARGET_HINT = '<div class="tip-label">Click to reveal it in the playbook.</div>';

/**
 * What a piece means, or undefined for a piece a reader does not need told about.
 *
 * A piece's own role explains it first: a menu's label holding a query is still a query, and the
 * reader asking what `{Gold}` means should not be told only where the option leads. A piece whose
 * role says nothing gets the word for the way out it stands for — and one that can be followed
 * always says so.
 */
function pieceTip(segment: PlaybookSegmentView, kind: string): string | undefined {
    const role = SUMMARY_SEGMENT_TIP[segment.role];
    const wayOut =
        segment.target === undefined ? undefined : (TARGET_TIP[kind] ?? TARGET_TIP_FALLBACK);
    const tip = role ?? wayOut;
    if (tip === undefined) return undefined;

    const meaning = edgeTooltipHtml(tip.name, tip.meaning, null);
    return segment.target === undefined ? meaning : meaning + TARGET_HINT;
}

/** One drawn piece: a segment the projection sent, with the class its role wears. */
function drawnSegment(segment: PlaybookSegmentView, kind: string): SemanticSegment {
    const className = SUMMARY_SEGMENT_CLASS[segment.role];
    const tip = pieceTip(segment, kind);

    return {
        text: segment.text,
        ...(className === undefined ? {} : { className }),
        ...(tip === undefined ? {} : { tip }),
        // A piece that names a node offers the two things the row's own number does: a click that
        // reveals the node, and a hover that lights up everything else naming it.
        ...(segment.target === undefined
            ? {}
            : {
                  target: { kind: "node", id: segment.target } satisfies PlaybookTarget,
                  refKey: `node:${segment.target}`,
              }),
    };
}

/**
 * A node's summary as a list: what reads before the first boundary introduces it, and each group
 * after a boundary is one item. Null for a summary that is a single line — one item is not a list,
 * and a summary with no boundaries is written as the one line it is.
 */
function summaryList(node: PlaybookNodeView): SemanticList | null {
    const lead: PlaybookSegmentView[] = [];
    const items: PlaybookSegmentView[][] = [];
    let current = lead;

    for (const segment of node.segments) {
        if (segment.role === "boundary") {
            current = [];
            items.push(current);
            continue;
        }
        current.push(segment);
    }

    if (items.length < 2) return null;

    return {
        lead: lead.map((segment) => drawnSegment(segment, node.kind)),
        items: items.map((item) => item.map((segment) => drawnSegment(segment, node.kind))),
        // A control's items are the steps it takes in order; a menu's are alternatives.
        ordered: node.kind === "control",
    };
}

/** The text of one drawn line, which is its pieces joined. */
function lineText(line: SemanticSegment[]): string {
    return line.map((segment) => segment.text).join("");
}

/**
 * The node's summary: the pieces the projection sent, joined for the cell's text and drawn by
 * role. A list — a menu's options, a control's commands — is drawn as the list it is, its
 * introduction on the cell's first line. Nothing is split here: a writer's own characters are
 * never part of the grammar, so a boundary is the projection's, never theirs.
 */
function summaryCell(node: PlaybookNodeView): SemanticCell {
    const list = summaryList(node);
    if (list) {
        const lines = list.lead.length > 0 ? [list.lead, ...list.items] : list.items;
        return { text: lines.map(lineText).join("\n"), list };
    }

    const segments = node.segments.map((segment) => drawnSegment(segment, node.kind));
    return {
        text: segments.map((segment) => segment.text).join(""),
        segments,
    };
}

/**
 * The nodes themselves: each one's position, the tag the document names it by, what it holds,
 * and where it leads — the table that answers "show me every choice", which the Kind column
 * makes a filter rather than a reading exercise.
 */
function nodeTable(nodes: readonly PlaybookNodeView[]): SemanticTable {
    return {
        title: "Nodes",
        columns: ["#", "Kind", "Summary", "Leads to"],
        rows: nodes.map((node) => {
            return {
                entityKey: `node:${node.id}`,
                cells: [
                    // The node's own position, so the cell takes the reader to that node in the
                    // JSON beside the table.
                    { text: String(node.id), jump: { kind: "node", id: node.id } },
                    // The kind wears the color the Dialogue Graph gives it, so the table and the
                    // drawing name a node the same way.
                    { text: node.kind, category: node.category },
                    // The words a writer wrote keep the plain colour while the report's own
                    // scaffolding steps back, so the speech is what the eye lands on.
                    summaryCell(node),
                    waysOut(node.targets),
                ],
            };
        }),
        emptyText: "This playbook holds no nodes.",
        // "Show me every choice" is the question this table makes answerable, and it is a
        // question about the node's kind.
        facetColumns: ["Kind"],
    };
}

/**
 * Where a node leads.
 *
 * One way out makes the whole cell the target. Several offer each one on its own, so a reader can
 * follow any of them rather than being handed whichever came first. None leaves the cell empty,
 * which the table never makes a link. The joined text is kept in every case, because that is what
 * the table's search and sort read. A way out also names the node it reaches, so hovering it lights
 * up that node's row.
 */
function waysOut(targets: readonly number[]): SemanticCell {
    const text = targets.join(", ");
    const [only] = targets;
    if (targets.length === 1 && only !== undefined) {
        return { text, refKey: `node:${only}`, jump: { kind: "node", id: only } };
    }
    if (targets.length > 1) {
        return {
            text,
            jumps: targets.map((target) => ({
                text: String(target),
                refKey: `node:${target}`,
                target: { kind: "node", id: target },
            })),
        };
    }
    return { text };
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
