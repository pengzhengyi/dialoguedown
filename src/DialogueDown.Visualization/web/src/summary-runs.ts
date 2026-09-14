/**
 * Splits a playbook node's single-line summary into styled runs for the table to draw.
 *
 * A summary is generated text rather than free prose, and each kind of node generates a different
 * shape: a line reads `speaker: speech`, a choice reads `option || option`, an end node has nothing
 * to say at all. The node's `kind` therefore names the grammar to apply, and that is what makes
 * splitting back into runs safe. The pieces holding a writer's own words — the speech of a line, an
 * option's label, the words on a divert — are taken whole and never scanned, so punctuation a
 * writer typed can never be mistaken for syntax.
 *
 * The split is total and lossless: the runs always concatenate back to the summary. Text the
 * grammar does not describe, including a summary cut short by a trailing ellipsis, comes back as a
 * single plain run.
 */

/** The styling role of one slice of a summary line. */
export type SummaryRole =
    "speaker" | "keyword" | "separator" | "command" | "query" | "absent" | "plain";

/** One styled slice of a summary line; concatenating the slices rebuilds the whole summary. */
export interface SummaryRun {
    text: string;
    role: SummaryRole;
}

const NODE_CONDITION = "IF ";
const NODE_CONSEQUENCE = " THEN ";
const TRUNCATION = "\u2026";
const DIVERT_ARROW = "\u21D2";
const CONTINUE = "CONTINUE";
const DRAW_PREFIX = "DRAW 1 FROM ";
const COMMAND_SEPARATOR = "; ";
const CHOICE_SEPARATOR = " || ";
const GUARD = " IF ";

/**
 * Splits `summary` into styled runs using the grammar of `kind`.
 *
 * Unknown kinds and text the grammar does not fit fall back to a single plain run, so callers can
 * pass any summary through without guarding the call.
 */
export function summaryRuns(summary: string, kind: string): SummaryRun[] {
    if (summary.endsWith(TRUNCATION)) {
        // A truncated construct can end anywhere, so no part of it can be trusted to parse.
        return plainRuns(summary);
    }
    if (kind === "branch") {
        // A branch summary legitimately starts with IF; that is not a node condition.
        return branchRuns(summary);
    }
    const condition = readNodeCondition(summary);
    if (condition === null) {
        return bodyRuns(summary, kind);
    }
    return [
        ...keywordRuns("IF"),
        ...plainRuns(" "),
        ...plainRuns(condition.key),
        ...plainRuns(" "),
        ...keywordRuns("THEN"),
        ...plainRuns(" "),
        ...bodyRuns(condition.body, kind),
    ];
}

/** A parsed `IF key THEN body` node condition. */
interface NodeCondition {
    key: string;
    body: string;
}

/**
 * Reads the leading `IF key THEN ` guard shared by every non-branch kind, or null when the summary
 * carries none.
 */
function readNodeCondition(summary: string): NodeCondition | null {
    if (!summary.startsWith(NODE_CONDITION)) {
        return null;
    }
    const marker = summary.indexOf(NODE_CONSEQUENCE, NODE_CONDITION.length);
    if (marker === -1) {
        return null;
    }
    return {
        key: summary.slice(NODE_CONDITION.length, marker),
        body: summary.slice(marker + NODE_CONSEQUENCE.length),
    };
}

/** Dispatches the summary that follows any node condition to the grammar for its kind. */
function bodyRuns(body: string, kind: string): SummaryRun[] {
    switch (kind) {
        case "line":
            return lineRuns(body);
        case "control":
            return controlRuns(body);
        case "choice":
            return listRuns(body, choiceEntryRuns);
        case "random-choice":
            return randomChoiceRuns(body);
        case "branch":
            return branchRuns(body);
        case "end":
            return keywordRuns(body);
        default:
            return plainRuns(body);
    }
}

/** `speaker: speech`, where only the speech may hold query placeholders. */
function lineRuns(body: string): SummaryRun[] {
    const divider = body.indexOf(": ");
    if (divider === -1) {
        return plainRuns(body);
    }
    return [
        ...speakerRuns(body.slice(0, divider)),
        ...separatorRuns(":"),
        ...plainRuns(" "),
        ...speechRuns(body.slice(divider + 2)),
    ];
}

/** Speech is opaque apart from its curly-brace placeholders; a bare bracketed value is absent. */
function speechRuns(speech: string): SummaryRun[] {
    if (isBracketed(speech)) {
        return absentRuns(speech);
    }
    return splitQueries(speech);
}

/** `CONTINUE`, a divert arrow, or a semicolon-separated command list. */
function controlRuns(body: string): SummaryRun[] {
    if (body === CONTINUE) {
        return keywordRuns(body);
    }
    if (body.startsWith(`${DIVERT_ARROW} `)) {
        return [
            ...separatorRuns(DIVERT_ARROW),
            ...plainRuns(" "),
            ...plainRuns(body.slice(DIVERT_ARROW.length + 1)),
        ];
    }
    return splitCommands(body);
}

/** Commands are separated by `; `, which is rebuilt run for run so the text survives intact. */
function splitCommands(body: string): SummaryRun[] {
    const runs: SummaryRun[] = [];
    body.split(COMMAND_SEPARATOR).forEach((text, index) => {
        if (index > 0) {
            runs.push(...separatorRuns(";"), ...plainRuns(" "));
        }
        runs.push(...commandRuns(text));
    });
    return runs;
}

/** Renders one option or odds entry in isolation, ignoring any trailing guard. */
type EntryRenderer = (entry: string) => SummaryRun[];

/** A ` || `-separated list; each entry is rendered by the grammar of the list's own kind. */
function listRuns(body: string, renderEntry: EntryRenderer): SummaryRun[] {
    const runs: SummaryRun[] = [];
    body.split(CHOICE_SEPARATOR).forEach((entry, index) => {
        if (index > 0) {
            runs.push(...plainRuns(" "), ...separatorRuns("||"), ...plainRuns(" "));
        }
        runs.push(...optionRuns(entry, renderEntry));
    });
    return runs;
}

/**
 * An entry's own words plus an optional trailing ` IF key` guard. The own words go back through the
 * kind's renderer so an option stays plain while an odds entry may still hold a query.
 */
function optionRuns(entry: string, renderEntry: EntryRenderer): SummaryRun[] {
    const guard = guardIndex(entry);
    if (guard === -1) {
        return renderEntry(entry);
    }
    return [
        ...renderEntry(entry.slice(0, guard)),
        ...plainRuns(" "),
        ...keywordRuns("IF"),
        ...plainRuns(" "),
        ...plainRuns(entry.slice(guard + GUARD.length)),
    ];
}

/** An option is one opaque label, or an absent marker when it is a bare bracketed value. */
function choiceEntryRuns(entry: string): SummaryRun[] {
    if (isBracketed(entry)) {
        return absentRuns(entry);
    }
    return plainRuns(entry);
}

/** An odds entry may carry one query placeholder; everything outside it is plain. */
function oddsRuns(entry: string): SummaryRun[] {
    return splitQueries(entry);
}

/** `DRAW 1 FROM count: odds || odds`, where each entry still follows the option rule. */
function randomChoiceRuns(body: string): SummaryRun[] {
    if (!body.startsWith(DRAW_PREFIX)) {
        return plainRuns(body);
    }
    const rest = body.slice(DRAW_PREFIX.length);
    const divider = rest.indexOf(": ");
    if (divider === -1) {
        return plainRuns(body);
    }
    return [
        // The count is part of the phrase, not a value worth finding: the odds listed after it
        // already say how many arms there are, so the whole phrase steps back together.
        ...keywordRuns(`${DRAW_PREFIX}${rest.slice(0, divider)}`),
        ...separatorRuns(":"),
        ...plainRuns(" "),
        ...listRuns(rest.slice(divider + 2), oddsRuns),
    ];
}

const BRANCH_WORDS = /(\bIF\b|\bTHEN\b|\bELSE\b)/;

/** A branch is condition text spattered with the whole words IF, THEN and ELSE. */
function branchRuns(summary: string): SummaryRun[] {
    const runs: SummaryRun[] = [];
    summary.split(BRANCH_WORDS).forEach((piece, index) => {
        if (piece.length === 0) {
            return;
        }
        runs.push({ text: piece, role: index % 2 === 0 ? "plain" : "keyword" });
    });
    return runs;
}

/** Splits text into curly-brace queries and the plain text between them. */
function splitQueries(text: string): SummaryRun[] {
    const runs: SummaryRun[] = [];
    let cursor = 0;
    while (cursor < text.length) {
        const open = text.indexOf("{", cursor);
        const close = open === -1 ? -1 : text.indexOf("}", open + 1);
        if (open === -1 || close === -1) {
            // No complete placeholder is left, so the remainder stays exactly as the writer typed it.
            runs.push(...plainRuns(text.slice(cursor)));
            break;
        }
        runs.push(...plainRuns(text.slice(cursor, open)));
        runs.push(...queryRuns(text.slice(open, close + 1)));
        cursor = close + 1;
    }
    return runs;
}

/** True for a value entirely wrapped in angle brackets, such as a nameless speaker. */
function isBracketed(text: string): boolean {
    if (!text.startsWith("<") || !text.endsWith(">")) {
        return false;
    }
    const inner = text.slice(1, -1);
    return !inner.includes("<") && !inner.includes(">");
}

/**
 * Finds the ` IF ` separating an entry from its trailing guard, skipping candidates that sit inside
 * a bracketed value so a writer's own words are never mistaken for a guard.
 */
function guardIndex(entry: string): number {
    let angleDepth = 0;
    let braceDepth = 0;
    let guard = -1;
    for (let index = 0; index < entry.length; index += 1) {
        if (entry.startsWith("<", index)) {
            angleDepth += 1;
        } else if (entry.startsWith(">", index)) {
            angleDepth = Math.max(0, angleDepth - 1);
        } else if (entry.startsWith("{", index)) {
            braceDepth += 1;
        } else if (entry.startsWith("}", index)) {
            braceDepth = Math.max(0, braceDepth - 1);
        }
        const insideValue = angleDepth > 0 || braceDepth > 0;
        // A guard must leave a key behind; otherwise it is part of the entry's own words.
        if (!insideValue && entry.startsWith(GUARD, index) && index + GUARD.length < entry.length) {
            guard = index;
        }
    }
    return guard;
}

function plainRuns(text: string): SummaryRun[] {
    return text.length === 0 ? [] : [{ text, role: "plain" }];
}

function keywordRuns(text: string): SummaryRun[] {
    return text.length === 0 ? [] : [{ text, role: "keyword" }];
}

function separatorRuns(text: string): SummaryRun[] {
    return [{ text, role: "separator" }];
}

function speakerRuns(text: string): SummaryRun[] {
    return text.length === 0 ? [] : [{ text, role: "speaker" }];
}

function commandRuns(text: string): SummaryRun[] {
    return text.length === 0 ? [] : [{ text, role: "command" }];
}

function queryRuns(text: string): SummaryRun[] {
    return [{ text, role: "query" }];
}

function absentRuns(text: string): SummaryRun[] {
    return [{ text, role: "absent" }];
}
