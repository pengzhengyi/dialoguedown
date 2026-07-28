/** Options mirroring the editor's Match Case / Match Whole Word search toggles. */
export interface MatchOptions {
    caseSensitive: boolean;
    wholeWord: boolean;
}

/** A half-open `[start, end)` range of one match within the searched text. */
export interface MatchRange {
    start: number;
    end: number;
}

const WORD_CHAR = /[\p{L}\p{N}_]/u;

function isWordChar(char: string | undefined): boolean {
    return char !== undefined && WORD_CHAR.test(char);
}

// A match is a whole word when a non-word character (or the string edge) sits on both sides,
// mirroring the editor's Match Whole Word.
function isWholeWord(text: string, start: number, end: number): boolean {
    return !isWordChar(text[start - 1]) && !isWordChar(text[end]);
}

/**
 * Every non-overlapping occurrence of `query` in `text`, honoring case sensitivity and whole-word
 * matching. An empty query matches nothing. This is the single source of truth for both the
 * table's search filter and its match highlighting, so the marks always match what the filter kept.
 */
export function findMatches(text: string, query: string, options: MatchOptions): MatchRange[] {
    if (query.length === 0) {
        return [];
    }

    const haystack = options.caseSensitive ? text : text.toLowerCase();
    const needle = options.caseSensitive ? query : query.toLowerCase();

    const ranges: MatchRange[] = [];
    for (let from = haystack.indexOf(needle); from >= 0; from = haystack.indexOf(needle, from)) {
        const end = from + needle.length;
        if (!options.wholeWord || isWholeWord(text, from, end)) {
            ranges.push({ start: from, end });
        }
        from = end; // non-overlapping: advance past this occurrence
    }
    return ranges;
}

/** Whether `text` contains at least one match for `query` under `options`. */
export function hasMatch(text: string, query: string, options: MatchOptions): boolean {
    return findMatches(text, query, options).length > 0;
}
