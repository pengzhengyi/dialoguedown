import { EditorState, Compartment } from "@codemirror/state";
import {
    EditorView,
    lineNumbers,
    keymap,
    drawSelection,
    highlightActiveLine,
    highlightActiveLineGutter,
} from "@codemirror/view";
import { history, historyKeymap, defaultKeymap } from "@codemirror/commands";
import { closeBrackets, closeBracketsKeymap } from "@codemirror/autocomplete";
import {
    StreamLanguage,
    syntaxHighlighting,
    HighlightStyle,
    foldGutter,
    codeFolding,
    foldKeymap,
    foldService,
    bracketMatching,
} from "@codemirror/language";
import { searchKeymap, highlightSelectionMatches } from "@codemirror/search";
import { toml } from "@codemirror/legacy-modes/mode/toml";
import { tags } from "@lezer/highlight";
import { delegate } from "tippy.js";
import { foldGutterMarker } from "./fold-glyph";
import type { ConfigReport, ConfiguredSpeakerView } from "./model";
import { isConfiguredFromFile } from "./model";
import { initSplitDivider } from "./source-view";
import { copyToClipboard } from "./path-display";
import { initCollapsiblePanel } from "./collapse-toggle";
import { showToast } from "./toast";
import { configCompletions } from "./config-completions";
import { compactSearch } from "./search-panel";
import { escapeHtml } from "./text";

/** Options for the Config tab. */
export interface ConfigViewOptions {
    /** Start the TOML editor editable (Edit) or read-only (View); toggled later via the handle. */
    editable?: boolean;
    /** Called with the new buffer on every edit — for the config buffer and dirty state. */
    onChange?: (source: string) => void;
    /**
     * Create a `dialogue.toml` for a project that has none. Shown as a call to action in the
     * no-config state, in Edit only; resolves once the create is under way (the caller
     * navigates), rejects with a reader-facing message on conflict or failure. Absent for the
     * static export (no server to write with).
     */
    onCreateConfig?: () => Promise<void>;
}

/**
 * A handle to the Config tab so the live-edit machinery can drive it: flip the TOML editor
 * between editable and read-only, replace its content (a discard/restore), refresh the
 * configured speakers after a recompile, and signpost the pane as stale while unsaved.
 */
export interface ConfigViewHandle {
    /** The Config tab element to mount. */
    readonly element: HTMLElement;
    /** Switch the TOML editor between editable (Edit) and read-only (View) in place. */
    setEditable(editable: boolean): void;
    /** Replace the editor's content — used to restore the last saved version on discard. */
    setContent(source: string): void;
    /** Re-render the mode row and configured speakers from a freshly recompiled report. */
    updateConfig(config: ConfigReport): void;
    /** Mark the speakers pane as out of date (unsaved edits) or up to date. */
    setStale(stale: boolean): void;
}

/**
 * TOML highlighting driven by the same CSS variables the Markdown editor uses, so the config
 * source follows the page's light/dark theme live.
 */
const tomlHighlightStyle = HighlightStyle.define([
    { tag: [tags.keyword, tags.definition(tags.propertyName)], color: "var(--md-heading)" },
    { tag: tags.string, color: "var(--md-code)" },
    { tag: [tags.number, tags.bool, tags.atom], color: "var(--md-link)" },
    { tag: [tags.comment, tags.lineComment], color: "var(--md-muted)", fontStyle: "italic" },
    { tag: [tags.bracket, tags.squareBracket], color: "var(--md-muted)" },
]);

// Match a TOML table or array-of-tables header line, e.g. `[owner]` or `[[speakers]]`.
const TOML_HEADER = /^\s*\[/;

/**
 * Fold a TOML table/array-of-tables section: from the header line to the last line before the
 * next header (or the end of the file). The StreamLanguage TOML mode has no syntax-tree
 * folding, so a small line-based service supplies it — the analog of the Source editor's
 * heading fold.
 */
const foldTomlSections = foldService.of((state, lineStart) => {
    const header = state.doc.lineAt(lineStart);
    if (!TOML_HEADER.test(header.text)) return null;
    let end = header.number;
    for (let n = header.number + 1; n <= state.doc.lines; n++) {
        if (TOML_HEADER.test(state.doc.line(n).text)) break;
        end = n;
    }
    return end > header.number ? { from: header.to, to: state.doc.line(end).to } : null;
});

/**
 * The Config tab: the applied configuration shown as a two-column split — the `dialogue.toml`
 * source (read-only, TOML-highlighted) on the left and the resolved configured speakers on
 * the right — reusing the Source tab's split machinery. When no config file was found it
 * shows a friendly explanation instead, because running on the built-in defaults is normal.
 */
export function createConfigView(
    config: ConfigReport,
    options: ConfigViewOptions = {},
): ConfigViewHandle {
    const container = document.createElement("div");
    container.className = "config-view";

    const pane = document.createElement("div");
    pane.className = "config-source";

    const divider = document.createElement("div");
    divider.className = "config-divider";

    const side = document.createElement("div");
    side.className = "config-side";

    // A quiet, hidden-by-default hint that marks the speakers as out of date while the TOML
    // has unsaved edits (the speakers only refresh on Save — see the Live Edit note, DD6).
    const staleHint = renderStaleHint();

    let editor: EditorView | null = null;
    let speakers: HTMLElement | null = null;
    let modeRow: HTMLElement | null = null;
    let noConfig: NoConfigControls | null = null;
    const reservedTags = config.reservedTags ?? [];
    modeRow = renderModeRow(config.mode);
    if (isConfiguredFromFile(config)) {
        editor = mountEditor(
            pane,
            config.file!.source,
            options.editable ?? false,
            reservedTags,
            options.onChange,
        );
        speakers = renderSpeakers(config.speakers);
        side.append(staleHint, modeRow, speakers);
    } else {
        noConfig = renderNoConfig(options.editable ?? false, options.onCreateConfig);
        pane.appendChild(noConfig.element);
        side.append(modeRow, renderNoSpeakers());
    }

    container.append(pane, divider, side);
    initSplitDivider(container, divider, "--config-split", "config-collapsed");

    // A portable Tippy tooltip on the mode value, delegated on the stable side panel so it keeps
    // working after the row is replaced on save. It anchors to the small value pill (not the
    // full-width row, whose center sits far right of the text) and replaces a native `title`,
    // which triggered unreliably and looks different on each platform.
    delegate(side, {
        target: ".config-mode-value",
        content: MODE_TOOLTIP,
        placement: "top-start",
        maxWidth: 320,
        delay: [120, 0],
    });

    // The right (speakers) panel can be hidden to give the config source the full width,
    // the same way the Source tab hides its preview. The toggle lives on the divider and
    // doubles as the always-present re-open handle; the choice is remembered across reloads.
    const speakersPanel = initCollapsiblePanel({
        container,
        collapsedClass: "config-collapsed",
        storageKey: "dd-config-collapsed",
        name: "configured speakers",
    });
    divider.appendChild(speakersPanel.button);

    return {
        element: container,
        setEditable: (next) => {
            editor?.dispatch({
                effects: editability.reconfigure(editableConfig(next, reservedTags)),
            });
            noConfig?.setEditable(next);
        },
        setContent: (next) =>
            editor?.dispatch({ changes: { from: 0, to: editor.state.doc.length, insert: next } }),
        updateConfig: (next) => {
            if (modeRow) {
                const freshMode = renderModeRow(next.mode);
                modeRow.replaceWith(freshMode);
                modeRow = freshMode;
            }
            if (!speakers || !isConfiguredFromFile(next)) return;
            const fresh = renderSpeakers(next.speakers);
            speakers.replaceWith(fresh);
            speakers = fresh;
        },
        setStale: (stale) => {
            staleHint.hidden = !stale;
        },
    };
}

/** The read-only-in-View / editable-in-Edit extensions, flipped at runtime via this compartment. */
const editability = new Compartment();

function editableConfig(editable: boolean, reservedTags: readonly string[]) {
    return [
        EditorState.readOnly.of(!editable),
        EditorView.contentAttributes.of(
            editable
                ? { "aria-label": "Configuration source editor", tabindex: "0" }
                : { "aria-label": "Configuration source", "aria-readonly": "true", tabindex: "0" },
        ),
        // The authoring aids (close-brackets, schema autocompletion) are Edit-only, so they
        // live in this compartment and vanish in read-only View.
        ...(editable ? [closeBrackets(), configCompletions(reservedTags)] : []),
    ];
}

// TOML table headers are `[[table]]`, so auto-closing `[` fights the writer (and would leave a
// stray `]` when accepting a `[[speakers]]` completion). Close only braces and quotes here.
const configCloseBrackets = EditorState.languageData.of(() => [
    { closeBrackets: { brackets: ["(", "{", '"', "'"] } },
]);

/** A focusable CodeMirror over the TOML source — read-only in View, editable in Edit. */
function mountEditor(
    parent: HTMLElement,
    source: string,
    editable: boolean,
    reservedTags: readonly string[],
    onChange?: (source: string) => void,
): EditorView {
    return new EditorView({
        parent,
        state: EditorState.create({
            doc: source,
            extensions: [
                lineNumbers(),
                highlightActiveLineGutter(),
                foldGutter({ markerDOM: foldGutterMarker }),
                foldTomlSections,
                codeFolding(),
                history(),
                drawSelection(),
                highlightActiveLine(),
                highlightSelectionMatches(),
                bracketMatching(),
                compactSearch(),
                configCloseBrackets,
                editability.of(editableConfig(editable, reservedTags)),
                StreamLanguage.define(toml),
                syntaxHighlighting(tomlHighlightStyle),
                EditorView.lineWrapping,
                keymap.of([
                    ...closeBracketsKeymap,
                    ...defaultKeymap,
                    ...historyKeymap,
                    ...searchKeymap,
                    ...foldKeymap,
                ]),
                EditorView.updateListener.of((update) => {
                    if (update.docChanged) onChange?.(update.state.doc.toString());
                }),
            ],
        }),
    });
}

/** The quiet "unsaved — save to refresh the speakers" hint (hidden until the config is dirty). */
function renderStaleHint(): HTMLElement {
    const hint = document.createElement("p");
    hint.className = "config-stale-hint";
    hint.hidden = true;
    hint.textContent = "Unsaved changes — save to refresh the speakers.";
    return hint;
}

/**
 * The mode row's tooltip: why the setting exists and how it relates to the report. Rendered by a
 * portable Tippy tooltip (delegated below) rather than a native `title`, which hovered
 * unreliably and varies across platforms.
 */
const MODE_TOOLTIP =
    "How this project compiles after an error — used by the dialoguedown CLI and embedded " +
    "builds. The visualization always renders stage-boundary, so every stage it shows is " +
    "built from reliable input; this setting doesn't change the report.";

/**
 * The project's configured compilation mode, shown above the speakers. A delegated Tippy tooltip
 * (see {@link MODE_TOOLTIP}) explains that the mode drives the CLI and embedded builds, while the
 * visualization always renders stage-boundary — so the report never shows a stage rebuilt from
 * post-error material.
 */
function renderModeRow(mode: string | undefined): HTMLElement {
    const row = document.createElement("div");
    row.className = "config-mode";
    const value = mode ?? "stage-boundary";
    row.innerHTML =
        `<span class="config-mode-label">Mode</span>` +
        `<span class="config-mode-value">${escapeHtml(value)}</span>`;
    return row;
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

/** A focusable, read-only CodeMirror showing the TOML source. */
/** The configured-speakers table: Name, Id, and tag chips colored by reserved vs custom. Every
 *  value is click-to-copy, so a writer can lift a name, `@id`, or tag straight into a script. */
function renderSpeakers(speakers: ConfiguredSpeakerView[]): HTMLElement {
    const wrapper = document.createElement("div");
    wrapper.className = "config-speakers";
    wrapper.innerHTML = `<h4 class="config-speakers-heading">Configured speakers</h4>`;

    if (speakers.length === 0) {
        wrapper.appendChild(renderNoSpeakers());
        return wrapper;
    }

    const rows = speakers.map((speaker) => `<tr>${speakerCells(speaker)}</tr>`).join("");
    const table = document.createElement("table");
    table.className = "semantic-table config-speakers-table";
    table.innerHTML =
        `<thead><tr>` +
        `<th scope="col">Name</th><th scope="col">Id</th><th scope="col">Tags</th>` +
        `</tr></thead><tbody>${rows}</tbody>`;
    wireClickToCopy(table);
    wrapper.appendChild(table);
    return wrapper;
}

/** A value cell whose displayed text is exactly what a click copies (name, `@id`, …). */
function copyCell(text: string): string {
    const safe = escapeHtml(text);
    return `<td class="config-copy" data-copy="${safe}" title="Click to copy">${safe}</td>`;
}

function speakerCells(speaker: ConfiguredSpeakerView): string {
    // The id is shown (and copied) with its `@` sigil, exactly as a script references it.
    const id = speaker.id
        ? copyCell(`@${speaker.id}`)
        : `<td><span class="config-empty">—</span></td>`;
    const tags =
        speaker.tags.length === 0
            ? `<td><span class="config-empty">—</span></td>`
            : `<td><div class="config-tags">${speaker.tags.map(tagChip).join(" ")}</div></td>`;
    return copyCell(speaker.name) + id + tags;
}

/** One tag chip, marked reserved or custom so CSS colors the two apart; click-to-copy. */
function tagChip(tag: { name: string; value?: string; reserved: boolean }): string {
    // Reserved tags are written with a double hash (`##default`), custom ones with a single.
    const prefix = tag.reserved ? "##" : "#";
    const label = tag.value == null ? `${prefix}${tag.name}` : `${prefix}${tag.name}=${tag.value}`;
    const kind = tag.reserved ? "reserved" : "custom";
    const safe = escapeHtml(label);
    return (
        `<span class="config-tag config-tag-${kind}" data-copy="${safe}" title="Click to copy">` +
        `${safe}</span>`
    );
}

/** A controller over the no-config pane: it flips its create call to action with View⇄Edit. */
interface NoConfigControls {
    readonly element: HTMLElement;
    /** Show the create button (Edit) or the quiet "switch to Edit" hint (View). */
    setEditable(editable: boolean): void;
}

// Feather Icons (MIT) `file-plus`, leading the create call to action.
const FILE_PLUS_ICON =
    '<svg class="config-create-icon" viewBox="0 0 24 24" width="15" height="15" fill="none"' +
    ' stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"' +
    ' aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />' +
    '<polyline points="14 2 14 8 20 8" /><line x1="12" y1="18" x2="12" y2="12" />' +
    '<line x1="9" y1="15" x2="15" y2="15" /></svg>';

/**
 * The friendly left pane when there is no config file — the built-in defaults are in play. When
 * the session can create one (a served session passes `onCreate`), it grows a call to action: a
 * create button in Edit, or a quiet "switch to Edit" hint in View. The static export passes no
 * `onCreate`, so it shows the explanation alone.
 */
function renderNoConfig(editable: boolean, onCreate?: () => Promise<void>): NoConfigControls {
    const note = document.createElement("div");
    note.className = "config-empty-state";
    note.innerHTML =
        `<p>This project has no <code>dialogue.toml</code>, so your script compiled with the ` +
        `built-in defaults.</p>` +
        `<p>Add one to declare speakers that every script can use — their names, ids, tags, ` +
        `and a default speaker.</p>`;

    if (!onCreate) {
        return { element: note, setEditable: () => {} };
    }

    const error = document.createElement("p");
    error.className = "config-create-error";
    error.hidden = true;

    const hint = document.createElement("p");
    hint.className = "config-create-hint";
    hint.textContent = "Switch to Edit to add one.";

    const button = document.createElement("button");
    button.type = "button";
    button.className = "config-create-button";
    button.innerHTML = `${FILE_PLUS_ICON}<span>Create dialogue.toml</span>`;
    button.addEventListener("click", () => {
        button.disabled = true;
        error.hidden = true;
        // On success the caller reloads onto the Config tab (the button stays disabled as the
        // page navigates); on conflict or failure, surface the message and re-enable.
        onCreate().catch((reason: unknown) => {
            error.textContent = reason instanceof Error ? reason.message : String(reason);
            error.hidden = false;
            button.disabled = false;
        });
    });

    note.append(button, hint, error);
    const controls: NoConfigControls = {
        element: note,
        setEditable: (next) => {
            button.hidden = !next;
            hint.hidden = next;
        },
    };
    controls.setEditable(editable);
    return controls;
}

/** The right pane's empty configured-speakers note. */
function renderNoSpeakers(): HTMLElement {
    const note = document.createElement("p");
    note.className = "config-empty";
    note.textContent = "No configured speakers yet.";
    return note;
}
