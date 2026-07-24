/** The display model produced by the .NET walk and serialized into the report. */

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
     * original document, so an edit can be spliced back into the exact source. Absent for a
     * synthetic node (no source of its own); the whole document for the document-root node.
     */
    span?: { start: number; end: number };
    /** A stable, cross-stage semantic category that drives color. */
    category?: string;
    /** A cross-link key tying the node to a semantic entity (a scene), if any. */
    entityKey?: string;
    /** The node's kind for the legend, when its label carries content (e.g. a scene title). */
    typeName?: string;
    /** A cross-link key when the node *references* an entity (a jump's scene, a speaker mention). */
    refKey?: string;
}

export type DisplayEdgeKind = "Child" | "Reference";

export interface DisplayEdge {
    fromId: string;
    toId: string;
    kind: DisplayEdgeKind;
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
    "SpeakerName" | "SpeakerId" | "Separator" | "CustomTag" | "ReservedTag" | "JumpIndicator";

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
}

/** The empty symbol set — no completions, used when a report carried no resolved symbols. */
export const EMPTY_SYMBOLS: DialogueSymbols = {
    jumpTargets: [],
    speakers: [],
    speakerIds: [],
    tags: [],
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

/** One tag of a {@link ConfiguredSpeakerView}; `reserved` colors reserved names apart from custom. */
export interface ConfiguredTagView {
    name: string;
    value?: string;
    reserved: boolean;
}

/** A configured speaker shown in the Config tab: a name, an optional id, and its tags. */
export interface ConfiguredSpeakerView {
    name: string;
    id?: string;
    tags: ConfiguredTagView[];
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
