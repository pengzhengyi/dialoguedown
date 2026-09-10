/**
 * The control inside a cell that does something — copying an identifier, revealing a place.
 *
 * A cell that acts when it is pressed is a control, and a control has to be reachable without a
 * mouse. A real `<button>` is what makes it one: it takes focus in document order, it turns Enter
 * and Space into a click of its own accord, and it announces itself as a button. Because the
 * activation arrives as an ordinary click, the delegated listeners the tables already use need no
 * second path for the keyboard.
 *
 * The button sits *inside* the cell rather than replacing it. A `<td>` given a button role stops
 * being a cell, and a reader moving through the table cell by cell would lose the table; nesting
 * keeps both the grid and the control.
 */

/** The class every in-cell control wears, so one rule can strip the browser's button chrome. */
export const CELL_ACTION_CLASS = "cell-action";

/**
 * A bare button to put a cell's own content inside.
 *
 * `label` names the *act*, not the value — "Copy @guide" rather than "@guide" — because a button's
 * name is what a screen reader reads when it lands there, and the value is already in the text.
 */
export function cellAction(label: string): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = CELL_ACTION_CLASS;
    button.setAttribute("aria-label", label);
    return button;
}
