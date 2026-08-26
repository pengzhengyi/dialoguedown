/** The display model produced by the .NET walk and serialized into the report. */

import type { PlaybookTarget } from "./playbook-jump";

/**
 * A half-open `[start, end)` character range into the original document. A zero-width span
 * (`start === end`) is a caret position rather than a selection.
 */
export interface Span {
    start: number;
    end: number;
}

export interface DisplayAttribute {
    name: string;
    value: string;
}

export interface DisplayNode {
    id: string;
    label: string;
    attributes: DisplayAttribute[];
    /** The original source text this node was produced from, if known. */
    source?: string;
    /**
     * The node's source location as a half-open `[start, end)` character range into the
     * original document, so a reader can be taken straight to the text a node came from. A
     * synthetic node (no source of its own) carries a zero-width span at the position where it
     * belongs (a caret); the whole document for the document-root node.
     */
    span?: Span;
    /** A stable, cross-stage semantic category that drives color. */
    category?: string;
    /** A cross-link key tying the node to a semantic entity (a scene), if any. */
    entityKey?: string;
    /** The node's kind for the legend, when its label carries content (e.g. a scene title). */
    typeName?: string;
    /** A cross-link key when the node *references* an entity (a jump's scene, a speaker mention). */
    refKey?: string;
    /**
     * The named area of the document this node sits in — a scene. Nodes sharing a region are
     * drawn inside one band named once, rather than each repeating the name under its label.
     */
    region?: string;
}

/** A named area of the document the stage's nodes sit in — a scene today, a file later. */
export interface DisplayRegion {
    name: string;
    /** What kind of grouping it is, as the compiler names it. */
    kind: string;
    /** The slug a divert names it by. */
    anchor?: string;
    /** Where the region is declared — a scene's heading — so a reader can be taken there. */
    span?: Span;
}

export type DisplayEdgeKind = "Child" | "Reference";

export interface DisplayEdge {
    fromId: string;
    toId: string;
    kind: DisplayEdgeKind;
    /** What the link means — a fall-through, a jump, a chosen arm — driving its color. */
    category?: string;
    /**
     * What the writer called this route, for a link that carries words of its own. A jump becomes
     * an edge and is not kept in the line it left, so this is where those words survive.
     */
    label?: string;
}

/** One cell of a {@link SemanticTable}. */
export interface SemanticCell {
    text: string;
    /** Set when the cell itself is a cross-linked entity. */
    entityKey?: string;
    /** Set when the cell references another entity (a jump's resolved scene). */
    refKey?: string;
    /** A cross-stage category for color. */
    category?: string;
    /** Present when the cell is a tag list: drawn as capsules instead of {@link text}. */
    tags?: TagView[];
    /**
     * Set when the cell is an identifier a writer would paste into a script — an `@id`, an
     * anchor, a jump target. Such a cell copies its text on click; prose cells do not.
     */
    copyable?: boolean;
    /**
     * Set when the cell summarizes a place in an accompanying document, so clicking it takes the
     * reader there. Bound where the table is built, because a sorted or filtered table no longer
     * has the row in the position the data came from.
     *
     * Client-authored: no projection emits it.
     */
    jump?: PlaybookTarget;
}

/** One row of a {@link SemanticTable}; `entityKey` names the entity the row represents. */
export interface SemanticRow {
    cells: SemanticCell[];
    entityKey?: string;
}

/** A table shown beside the scene-tree graph in the Semantic tab. */
export interface SemanticTable {
    title: string;
    columns: string[];
    rows: SemanticRow[];
    /** Shown when there are no rows. */
    emptyText: string;
    /** Names of categorical columns the editor offers as a faceted filter (e.g. a jump's Type). */
    facetColumns?: string[];
}

export interface Stage {
    title: string;
    /** A one-line description of what this stage's graph shows (its tab tooltip). */
    description: string;
    nodes: DisplayNode[];
    edges: DisplayEdge[];
    /**
     * Optional tables shown beside the graph — the Semantic tab's speaker, anchor, and
     * jump-resolution tables. Absent for a plain graph stage.
     */
    tables?: SemanticTable[];
    /** The named areas the nodes sit in. Absent for a stage with no grouping to show. */
    regions?: DisplayRegion[];
    /**
     * Whether a `Child` edge nests the child's source inside the parent's — true for the syntax
     * trees, where a container's span is only its header and its reach comes from its children.
     * False for the Dialogue Graph, whose `Child` edges mark the spanning tree it is drawn with
     * rather than containment. Absent means it nests.
     */
    nests?: boolean;
    /**
     * Present when the stage's artifact was not produced (a halted compile). The stage
     * renders as a disabled tab; `nodes`/`edges` are empty.
     */
    unavailable?: StageUnavailable;
}

/** Why a stage's tab is disabled — its artifact was not produced (a halted compile). */
export interface StageUnavailable {
    /** A short, reader-facing reason, shown as the disabled tab's tooltip. */
    reason: string;
}

/**
 * The report payload the .NET library injects: the compiled source document and
 * each stage's display graph.
 */
/**
 * The served-mode project context behind the Explorer sidebar: the project `root` to display and
 * the active script's root-relative `activePath` to highlight and reveal in the tree, or `null`
 * when no document is active (the served shell's empty state).
 */
export interface ReportProject {
    root: string;
    activePath?: string;
}

/** One entry in the playbook's metadata table — a label and the value the runtime will read. */
export interface PlaybookMetadataView {
    /** The script the playbook was compiled from. */
    script: string;
    /** The playbook format's version. Zero while the format is pre-1.0. */
    formatVersion: number;
    /** Where the format this playbook was written to is published. */
    schemaUrl: string;
    /** Capabilities a host must provide to run this playbook. */
    requires: string[];
    /** Optional capabilities the playbook takes advantage of when the host has them. */
    uses: string[];
    /** The index of the node the runtime starts at. */
    entry: number;
    /** How many nodes the playbook holds. */
    nodeCount: number;
    /** How many named anchors a jump can target. */
    anchorCount: number;
}

/** One row of the playbook's speaker table. */
export interface PlaybookSpeakerView {
    /** The stable id a runtime looks this speaker up by. Absent when the speaker has none. */
    id?: string;
    /** The speaker's display name. Absent for the anonymous default speaker. */
    name?: string;
    /** Whether this is the speaker a line with no prefix belongs to. */
    default: boolean;
    /** The speaker's tags, each drawn as a capsule. */
    tags: TagView[];
}

/** One row of the playbook's anchor table. */
export interface PlaybookAnchorView {
    /** The anchor's slug, as a jump writes it. */
    name: string;
    /** The node position the anchor resolves to. */
    node: number;
}

/**
 * The compiled playbook — the runtime's artifact — shown in the Playbook tab: the serialized
 * JSON a host would load, beside the tables that summarize it.
 */
export interface PlaybookReport {
    /** The serialized playbook, indented for reading. Absent when no playbook was produced. */
    json?: string;
    /** The playbook's header, shown as a table. Absent when no playbook was produced. */
    metadata?: PlaybookMetadataView;
    /** The playbook's speakers, shown as a table. */
    speakers: PlaybookSpeakerView[];
    /** The playbook's anchors, shown as a table. */
    anchors: PlaybookAnchorView[];
    /** Why no playbook exists, when the compile did not reach one. */
    unavailable?: string;
}

export interface Report {
    /**
     * The original source document, shown in the Source tab. Absent when a single
     * graph is rendered on its own (no source to show).
     */
    source?: string;
    stages: Stage[];
    /** The document's path — present when the CLI or server knows the file. */
    path?: string;
    /** How the report is shown; drives the mode badge and whether to go live. */
    mode?: VisualizationMode;
    /**
     * The served-mode project context that backs the Explorer sidebar — the project root's
     * display path and the active script's root-relative path. Present only for a served,
     * browsable report; absent in the static export, so its presence gates the sidebar.
     */
    project?: ReportProject;
    /**
     * The semantic analyzer's resolved symbols (canonical speaker ids, merged tags,
     * validated jump targets), used to seed the Source editor's autocompletion.
     * Absent when the semantic stage did not run or produced nothing.
     */
    symbols?: DialogueSymbols;
    /**
     * The applied configuration, shown in the Config tab. Present when the report has a
     * configuration context (a CLI or served report); absent for a bare library render.
     */
    configuration?: ConfigReport;
    /**
     * The compiled playbook, shown in the Playbook tab after the Dialogue Graph. Present when
     * the report was built through the compiler; absent for a bare graph render.
     */
    playbook?: PlaybookReport;
    /**
     * The compiler's diagnostics in Language Server Protocol shape, rendered as the Source
     * editor's overlay (squiggles, gutter markers, tooltips). Present for a served or CLI
     * report; an empty array after a clean compile clears the overlay. Absent for a bare
     * library render that carried no diagnostics.
     */
    diagnostics?: LspDiagnostic[];
    /**
     * Set to `saved-invalid` when the served configuration is persisted but invalid: the report's
     * graphs and speakers are the last valid compile, {@link Report.configuration}'s file source is
     * the current invalid TOML, and {@link Report.configMessage} explains the parse error. The
     * client initializes the Config controller into the saved-invalid (report stale) state so a
     * page reload restores it instead of the last valid text.
     */
    configStatus?: "saved-invalid";
    /** The configuration parse error shown when {@link Report.configStatus} is `saved-invalid`. */
    configMessage?: string;
    /**
     * The compiler's semantic tokens, rendered as the Source editor's dialogue highlighting
     * (speakers, tags, jump indicators) layered over the Markdown colors. Present for a
     * served or CLI report; an empty array for a document with no dialogue constructs. Absent
     * for a bare library render that carried none.
     */
    semanticTokens?: SemanticToken[];
}

/** A zero-based position in the source (LSP shape): a line and a UTF-16 character offset. */
export interface LspPosition {
    line: number;
    character: number;
}

/** A half-open source range as LSP defines it — `start` up to (but not including) `end`. */
export interface LspRange {
    start: LspPosition;
    end: LspPosition;
}

/** LSP diagnostic severity: 1 error, 2 warning, 3 information, 4 hint. */
export type LspSeverity = 1 | 2 | 3 | 4;

/**
 * One diagnostic in Language Server Protocol shape, as the compiler projects it into the
 * report payload: a zero-based {@link LspRange}, an integer {@link LspSeverity}, the error
 * {@link LspDiagnostic.code}, the rendered {@link LspDiagnostic.message}, and the producing
 * {@link LspDiagnostic.source} (`"dialoguedown"`). A future language server publishes the
 * identical structure, so the editor overlay consumes it unchanged.
 */
export interface LspDiagnostic {
    range: LspRange;
    severity: LspSeverity;
    code: string;
    message: string;
    source: string;
}

/** The legend of dialogue-specific token kinds the compiler projects for highlighting. */
export type TokenKind =
    | "SpeakerName"
    | "SpeakerId"
    | "Separator"
    | "CustomTag"
    | "ReservedTag"
    | "JumpIndicator"
    | "ReservedAnchor"
    | "ControlKeyword"
    | "Query"
    | "Condition"
    | "StaticWeight"
    | "DynamicWeight"
    | "Command"
    | "IgnoredMarkdown";

/**
 * One positioned dialogue token the compiler projects from the parse: a zero-based
 * {@link LspRange} and its {@link TokenKind}. The Source editor renders it as a decoration
 * layered over the Markdown highlighting. A future language server publishes the identical
 * structure as its `semanticTokens`, so the editor consumes it unchanged.
 */
export interface SemanticToken {
    range: LspRange;
    kind: TokenKind;
}

/** One completable jump destination: a scene heading's anchor and its display text. */
export interface JumpTarget {
    /** The GitHub-style slug inserted after `#` — the same anchor the preview links to. */
    slug: string;
    /** The heading text, shown as the completion's detail. */
    heading: string;
}

/** The structural role a language-owned jump target plays in a dialogue run. */
export type ReservedTargetRole = "Entry" | "Terminal";

/** A language-owned jump target presented outside the source document. */
export interface ReservedTarget {
    /** The reserved anchor without its leading `#`. */
    anchor: string;
    /** The concise title shown in the fixed editor panel. */
    label: string;
    /** Whether the target begins or terminates a run. */
    role: ReservedTargetRole;
}

/**
 * The names a document contains, grouped by the DSL concept each completes — the semantic
 * analyzer's resolved symbols (canonical speaker ids, merged tags, validated jump targets)
 * carried in the report payload and read by the Source editor's autocompletion.
 */
export interface DialogueSymbols {
    /** Scene-heading anchors, for completing a jump destination `](#…)`. */
    jumpTargets: JumpTarget[];
    /** Speaker display names, for completing a line's leading speaker. */
    speakers: string[];
    /** Speaker stable ids (without the `@`), for completing `@id`. */
    speakerIds: string[];
    /** Speaker/line tags (without the `#`), for completing `#tag`. */
    tags: string[];
    /** Language-owned targets such as the terminal `#END`, independent of source headings. */
    reservedTargets: ReservedTarget[];
}

/** The empty symbol set — no completions, used when a report carried no resolved symbols. */
export const EMPTY_SYMBOLS: DialogueSymbols = {
    jumpTargets: [],
    speakers: [],
    speakerIds: [],
    tags: [],
    reservedTargets: [],
};

/**
 * Where the Source editor's autocompletion reads its symbols. Read on every completion so a
 * hot-reload or save refreshes the list by swapping the holder the provider closes over.
 */
export type DialogueSymbolProvider = () => DialogueSymbols;

/** The mode a report is shown in (mirrors the .NET `VisualizationMode`). */
export type VisualizationMode = "static" | "view" | "edit";

/** The two interactive modes of a served session, toggled in the browser (Vim-like). */
export type ServedMode = "view" | "edit";

/**
 * One speaker tag, wherever the report shows it. `reserved` marks a name DialogueDown owns
 * (such as `default`) apart from one the writer invented; keeping `name` and `value` apart is
 * what lets a capsule color by identity and copy the tag as written.
 */
export interface TagView {
    name: string;
    value?: string;
    reserved: boolean;
}

/** A configured speaker shown in the Config tab: a name, an optional id, and its tags. */
export interface ConfiguredSpeakerView {
    name: string;
    id?: string;
    tags: TagView[];
}

/**
 * The applied configuration shown in the Config tab: the `dialogue.toml` file (when one was
 * found) and the resolved configured speakers. An absent {@link ConfigReport.file} is the
 * no-config state — the compiler used its built-in defaults.
 */
export interface ConfigReport {
    file?: { path: string; source: string };
    /**
     * The project's configured compilation mode (its author-facing name, e.g. `stage-boundary`
     * or `best-effort`), shown in the tab. The mode governs the CLI and embedded builds; the
     * visualization itself always renders stage-boundary.
     */
    mode?: string;
    speakers: ConfiguredSpeakerView[];
    /** The reserved tag names the compiler recognizes (for the editor's autocompletion). */
    reservedTags?: string[];
}

/** Whether a `dialogue.toml` was found and applied (as opposed to the defaults). */
export function isConfiguredFromFile(config: ConfigReport): boolean {
    return config.file != null;
}
