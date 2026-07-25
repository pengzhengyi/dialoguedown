import { delegate } from "tippy.js";
import { copyToClipboard } from "./path-display";
import { showToast } from "./toast";

/** The chain-link glyph (GitHub's octicon-link) for the jump-link affordance. */
const LINK_ICON =
    '<svg viewBox="0 0 16 16" width="0.9em" height="0.9em" aria-hidden="true" focusable="false">' +
    '<path fill="currentColor" d="M7.775 3.275a.75.75 0 0 0 1.06 1.06l1.25-1.25a2 2 0 1 1 2.83 ' +
    "2.83l-2.5 2.5a2 2 0 0 1-2.83 0 .75.75 0 0 0-1.06 1.06 3.5 3.5 0 0 0 4.95 0l2.5-2.5a3.5 3.5 " +
    "0 0 0-4.95-4.95l-1.25 1.25Zm-4.69 9.64a2 2 0 0 1 0-2.83l2.5-2.5a2 2 0 0 1 2.83 0 .75.75 0 0 " +
    "0 1.06-1.06 3.5 3.5 0 0 0-4.95 0l-2.5 2.5a3.5 3.5 0 0 0 4.95 4.95l1.25-1.25a.75.75 0 0 0-1." +
    '06-1.06l-1.25 1.25a2 2 0 0 1-2.83 0Z"/></svg>';

/**
 * The Markdown jump link the chain-link affordance copies — `[text](#slug)`, ready to paste after
 * a `=>` choice. Exported for testing.
 */
export function headingJumpLink(text: string, slug: string): string {
    return `[${text}](#${slug})`;
}

/** The link affordance whose tooltip and clipboard payload are both {@link copy}. */
function linkButton(copy: string, label: string): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "heading-anchor heading-anchor-link";
    button.dataset.copy = copy;
    button.setAttribute("aria-label", label);
    return button;
}

/**
 * Add one hover-revealed chain-link affordance to every heading in `container` that carries a
 * non-empty id (its GitHub-style slug). The link copies the full jump target
 * `[heading](#slug)`, and its tooltip previews that exact value. Idempotent: re-running after a
 * preview re-render skips annotated headings, and a heading whose slug is empty (never a jump
 * target) gets no affordance. The inline SVG has no text content, so the heading's textContent
 * stays the plain title.
 */
export function annotateHeadingAnchors(container: HTMLElement): void {
    const headings = container.querySelectorAll<HTMLHeadingElement>("h1, h2, h3, h4, h5, h6");
    for (const heading of headings) {
        const slug = heading.id;
        if (!slug || heading.querySelector(".heading-anchor")) continue;
        const text = (heading.textContent ?? "").trim();

        const link = linkButton(headingJumpLink(text, slug), `Copy jump link to ${text}`);
        link.innerHTML = LINK_ICON;
        heading.append(link);
    }
}

/**
 * Wire the heading links' click and hover once on the stable `container` (the preview
 * element, whose innerHTML is replaced on every render). A click copies the affordance's value —
 * the full jump link — and confirms through the shared toast; hovering previews that same value
 * in a Tippy tooltip.
 */
export function wireHeadingAnchorCopy(container: HTMLElement): void {
    container.addEventListener("click", (event) => {
        const anchor = (event.target as Element | null)?.closest<HTMLElement>(".heading-anchor");
        const value = anchor?.dataset.copy;
        if (!value) return;
        void copyToClipboard(value).then(() => showToast(`Copied ${value}`));
    });
    delegate(container, {
        target: ".heading-anchor",
        content: (reference) => (reference as HTMLElement).dataset.copy ?? "",
    });
}
