/**
 * A VS Code codicon glyph as a decorative `<span>`. An empty name renders just the spacer class
 * (used to align rows that carry no leading icon under rows that do). The glyph is decorative, so
 * it is hidden from assistive technology.
 */
export function codicon(name: string, extraClass: string): HTMLElement {
    const span = document.createElement("span");
    span.className = name === "" ? extraClass : `codicon codicon-${name} ${extraClass}`;
    span.setAttribute("aria-hidden", "true");
    return span;
}
