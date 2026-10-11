/**
 * @file Go to line, shaped like VS Code's: a small box that floats over the text with the field on
 * one line and a sentence under it saying what pressing Enter will do. No button — Enter goes,
 * Escape and clicking away dismiss. The sentence is what teaches the reader the expression syntax.
 *
 * CodeMirror's own `gotoLine` calls `showDialog` without `content` or `top`, so its dialog has a
 * button, sits at the bottom, and has no second line. This module therefore renders the dialog and
 * parses the expression itself. {@link resolve} computes both the sentence and the position the
 * cursor goes to, so the dialog never promises a line it does not then go to.
 */

import { EditorSelection, type EditorState } from "@codemirror/state";
import { EditorView, showDialog, type Command, type KeyBinding } from "@codemirror/view";

/** Where a Go to line expression lands, once resolved against the document. */
interface GotoTarget {
    /** A line number, already clamped into the document. */
    readonly line: number;
    /** Whether the expression named a line outside the document and was pulled back inside. */
    readonly clampedLine: boolean;
    /** A one-based column, already clamped into the line. One when none was typed. */
    readonly column: number;
    /** The last column the line has — one past its final character, where a cursor may rest. */
    readonly lastColumn: number;
    /** Whether a column was asked for but not yet typed, as in `23:`. */
    readonly awaitingColumn: boolean;
    /** Whether a typed column ran past the end of the line and was pulled back. */
    readonly clampedColumn: boolean;
}

/**
 * A line, optionally signed for a relative jump, optionally suffixed `%` for a position in the
 * document, and optionally followed by `:column`: `12`, `+10`, `-10`, `50%`, `12:5`. The same
 * expression CodeMirror's own `gotoLine` accepts, except that the colon may stand alone (`23:`)
 * while the reader is still typing the column, so the dialog can prompt for it.
 */
const EXPRESSION = /^\s*([+-])?(\d+)?(?::(\d*))?(%)?\s*$/;

/** Resolves a typed expression, or null when it names nothing to go to. */
export function resolve(state: EditorState, value: string): GotoTarget | null {
    const match = EXPRESSION.exec(value);
    if (match == null) return null;
    const [, sign, digits, columnDigits, percent] = match;
    if (digits == null && columnDigits == null) return null;

    const start = state.doc.lineAt(state.selection.main.head);
    let line = digits == null ? start.number : Number(digits);
    if (digits != null && percent != null) {
        let fraction = line / 100;
        if (sign != null) {
            fraction = fraction * (sign === "-" ? -1 : 1) + start.number / state.doc.lines;
        }
        line = Math.round(state.doc.lines * fraction);
    } else if (digits != null && sign != null) {
        line = line * (sign === "-" ? -1 : 1) + start.number;
    }

    const clamped = Math.max(1, Math.min(state.doc.lines, line));
    const lastColumn = state.doc.line(clamped).length + 1;
    const typed = columnDigits == null || columnDigits === "" ? null : Number(columnDigits);
    const column = typed == null ? 1 : Math.max(1, Math.min(lastColumn, typed));
    return {
        line: clamped,
        clampedLine: clamped !== line,
        column,
        lastColumn,
        awaitingColumn: columnDigits === "",
        clampedColumn: typed != null && column !== typed,
    };
}

/**
 * The sentence under the field: what Enter will do, or what to type when it would do nothing.
 *
 * It teaches as it goes. With nothing typed it names the range and the forms; with a line but no
 * column it offers the column, which is the part of the expression a reader is most likely to
 * want next and least likely to guess.
 */
export function guidanceFor(state: EditorState, value: string): string {
    const target = resolve(state, value);
    if (target == null) {
        return `Type a line between 1 and ${state.doc.lines} — or 12:5, +10, -10, 50%.`;
    }
    if (target.awaitingColumn) {
        // An empty line has one column and no range to speak of, so say what is there instead.
        return target.lastColumn === 1
            ? `Line ${target.line} is empty — Enter goes to its start.`
            : `Type a column between 1 and ${target.lastColumn} on line ${target.line}.`;
    }
    if (target.column > 1 || target.clampedColumn) {
        const where = `line ${target.line} at column ${target.column}`;
        return target.clampedColumn
            ? `Press Enter to go to ${where}, the end of the line.`
            : `Press Enter to go to ${where}.`;
    }
    const where = target.clampedLine
        ? `line ${target.line}, the nearest in the document`
        : `line ${target.line}`;
    return `Press Enter to go to ${where} — add :5 for a column.`;
}

/**
 * Moves the cursor to a resolved target and brings it into view.
 *
 * Columns are one-based, as they are in VS Code and in the dialog's sentence; CodeMirror counts
 * them from zero, so `12:1` is offset by one to land on the line's first character.
 */
function goTo(view: EditorView, target: GotoTarget): void {
    const line = view.state.doc.line(target.line);
    const selection = EditorSelection.cursor(line.from + (target.column - 1));
    view.dispatch({
        selection,
        effects: EditorView.scrollIntoView(selection.from, { y: "center" }),
        scrollIntoView: true,
    });
}

/** The dialog's body: the field, then the sentence that tracks it. */
function dialogBody(view: EditorView, close: () => void): HTMLElement {
    const form = document.createElement("form");
    form.className = "dd-goto-form";

    const field = document.createElement("input");
    field.type = "text";
    field.name = "line";
    // Deliberately not `cm-textfield`: CodeMirror's base style for that class is light-themed and
    // is injected after this stylesheet, so it would paint a white field on the dark theme.
    field.className = "dd-goto-input";
    field.setAttribute("aria-label", "Go to line");
    field.value = String(view.state.doc.lineAt(view.state.selection.main.head).number);

    const guidance = document.createElement("div");
    guidance.className = "dd-goto-guidance";
    // Announced politely so the sentence reaches a screen reader as the field is typed into.
    guidance.setAttribute("role", "status");
    guidance.textContent = guidanceFor(view.state, field.value);

    field.addEventListener("input", () => {
        guidance.textContent = guidanceFor(view.state, field.value);
    });

    // Clicking away dismisses it, the way a quick input does. The check is deferred because focus
    // is briefly nowhere while it moves, and skipped when it lands back inside the dialog.
    form.addEventListener("focusout", () => {
        setTimeout(() => {
            if (!form.contains(form.ownerDocument.activeElement)) close();
        }, 0);
    });

    form.append(field, guidance);
    return form;
}

/**
 * Opens the dialog. Enter and Escape are wired by `showDialog` because the content holds a form;
 * the jump itself runs when that form resolves.
 */
export const gotoLine: Command = (view) => {
    const { result } = showDialog(view, {
        content: (dialogView, close) => dialogBody(dialogView, close),
        class: "dd-goto",
        top: true,
        focus: "input",
    });
    result.then((form) => {
        if (form == null) return;
        const field = form.elements.namedItem("line");
        const target = field instanceof HTMLInputElement ? resolve(view.state, field.value) : null;
        if (target != null) goTo(view, target);
    }, console.error);
    return true;
};

/**
 * Opens this dialog on VS Code's Go to Line binding, `Ctrl-g`, and on CodeMirror's own
 * `Mod-Alt-g`.
 *
 * `Ctrl-g` is literal Control on every platform, which is exactly what VS Code binds — on macOS
 * `Cmd-g` stays Find Next there as it does here, and on Windows and Linux `F3` keeps Find Next
 * when this takes `Ctrl-g` over. The editors list this keymap before `searchKeymap` so it wins
 * that overlap.
 */
export const gotoLineKeymap: readonly KeyBinding[] = [
    { key: "Ctrl-g", run: gotoLine, preventDefault: true },
    { key: "Mod-Alt-g", run: gotoLine, preventDefault: true },
];
