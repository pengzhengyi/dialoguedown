import {
    autocompletion,
    acceptCompletion,
    completionKeymap,
    snippetCompletion,
    type Completion,
    type CompletionContext,
    type CompletionResult,
    type CompletionSource,
} from "@codemirror/autocomplete";
import { keymap } from "@codemirror/view";
import type { Extension } from "@codemirror/state";
import { type DialogueSymbolProvider, EMPTY_SYMBOLS } from "./model";

// A completion's `type` selects its tooltip icon. These are custom, dialogue-specific types
// (not CodeMirror's built-in `variable`/`property`/…), styled with line icons in styles.css:
// a person for a speaker, `@` for an id, `#` for a tag, and an arrow for a jump target.
const JUMP = "dd-jump";
const SPEAKER = "dd-speaker";
const SPEAKER_ID = "dd-speaker-id";
const TAG = "dd-tag";

/**
 * Assemble a completion result at `from`, dropping the word being typed. The half-typed
 * token is itself scanned as a symbol (e.g. typing `@gu` scans `gu`), so suggesting it
 * back is noise; excluding it keeps the tooltip to real, other candidates.
 */
export function completionsFrom(
    context: CompletionContext,
    from: number,
    options: Completion[],
    validFor: RegExp,
): CompletionResult | null {
    const typed = context.state.sliceDoc(from, context.pos);
    const filtered = options.filter((option) => option.label !== typed);
    return filtered.length ? { from, options: filtered, validFor } : null;
}

/**
 * Complete the jump indicator `=>` with a whole jump target. Offers every scene by its heading
 * and, on accept, enriches into `[Heading](#slug)` — the heading as an editable snippet field
 * (Tab past it), the compiler-correct slug fixed. Fires before the link brackets are typed; once
 * the writer is inside `](#…)` the {@link jumpSlugCompletions} source takes over. A single space
 * always follows `=>`, so the result reads `=> [..]`, never `=>[..]`.
 */
export function jumpIndicatorCompletions(symbols: DialogueSymbolProvider): CompletionSource {
    return (context) => {
        const match = context.matchBefore(/=>[ \t]*[^[\]()\n]*/);
        if (!match) return null;
        const { jumpTargets } = symbols();
        if (!jumpTargets.length) return null;
        const labelStart = /^=>[ \t]*/.exec(match.text)![0].length;
        const space = /^=>[ \t]/.test(match.text) ? "" : " ";
        const options = jumpTargets.map(({ slug, heading }) =>
            snippetCompletion(`${space}[\${1:${heading}}](#${slug})`, {
                label: heading,
                detail: `#${slug}`,
                info: `[${heading}](#${slug})`,
                type: JUMP,
            }),
        );
        return { from: match.from + labelStart, options, validFor: /^[^[\]()\n]*$/ };
    };
}

/** Complete a jump destination `](#…)` typed by hand with the slug of any scene heading. */
export function jumpSlugCompletions(symbols: DialogueSymbolProvider): CompletionSource {
    return (context) => {
        const match = context.matchBefore(/\]\(#[\w-]*/);
        if (!match) return null;
        const { jumpTargets } = symbols();
        const options = jumpTargets.map((t) => ({ label: t.slug, detail: t.heading, type: JUMP }));
        return completionsFrom(context, match.from + 3, options, /^[\w-]*$/); // after `](#`
    };
}

/** Complete `@id` with the speaker ids declared or referenced in the document. */
export function speakerIdCompletions(symbols: DialogueSymbolProvider): CompletionSource {
    return (context) => {
        const match = context.matchBefore(/@[\w-]*/);
        if (!match) return null;
        const { speakerIds } = symbols();
        const options = speakerIds.map((id) => ({ label: id, type: SPEAKER_ID }));
        return completionsFrom(context, match.from + 1, options, /^[\w-]*$/); // after `@`
    };
}

/** Complete a mid-line `#tag` with the tags used in the document. */
export function tagCompletions(symbols: DialogueSymbolProvider): CompletionSource {
    return (context) => {
        const match = context.matchBefore(/#[\w-]*/);
        if (!match) return null;
        const line = context.state.doc.lineAt(match.from);
        const column = match.from - line.from;
        // A line-start hash is a Markdown heading; `](#` is a jump destination. Neither
        // is a tag, so leave those to the jump-target source (or nothing).
        const beforeHash = line.text.slice(0, column);
        if (beforeHash.trim() === "" || line.text[column - 1] === "(") return null;
        const { tags } = symbols();
        const options = tags.map((t) => ({ label: t, type: TAG }));
        return completionsFrom(context, match.from + 1, options, /^[\w-]*$/); // after `#`
    };
}

/**
 * Complete a line's leading speaker name. Returns every known speaker (CodeMirror filters
 * by the typed prefix), so it stays quiet on prose lines: a leading word that prefixes no
 * known speaker shows nothing.
 */
export function speakerCompletions(symbols: DialogueSymbolProvider): CompletionSource {
    return (context) => {
        const match = context.matchBefore(/^[ \t]*[A-Za-z][\w'’-]*/);
        if (!match) return null;
        const { speakers } = symbols();
        const indent = /^[ \t]*/.exec(match.text)![0].length;
        const options = speakers.map((s) => ({ label: s, type: SPEAKER }));
        return completionsFrom(context, match.from + indent, options, /^[\w'’-]*$/);
    };
}

/**
 * The Source editor's completion: jump targets, speaker ids, tags, and speaker names, drawn
 * from the compiler's resolved {@link DialogueSymbolProvider} (the report payload's symbols).
 * Because the list is compiler-correct, a completion can never suggest a name the compiler
 * would reject. Bundles CodeMirror's completion keymap so accept/dismiss keys work only where
 * this extension is active (the editor's Edit-only compartment). Adds Tab as a second accept
 * key alongside Enter (the VS Code habit); with no completion open it falls through, so Tab
 * indents the line instead (see `indentWithTab` in `source-view.ts`).
 */
export function dialogueAutocompletion(
    symbols: DialogueSymbolProvider = () => EMPTY_SYMBOLS,
): Extension {
    return [
        autocompletion({
            override: [
                jumpIndicatorCompletions(symbols),
                jumpSlugCompletions(symbols),
                speakerIdCompletions(symbols),
                tagCompletions(symbols),
                speakerCompletions(symbols),
            ],
        }),
        keymap.of([...completionKeymap, { key: "Tab", run: acceptCompletion }]),
    ];
}
