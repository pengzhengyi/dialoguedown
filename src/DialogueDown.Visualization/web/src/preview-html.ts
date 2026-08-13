import DOMPurify from "dompurify";

/**
 * Sanitize rendered Markdown without erasing the metadata DialogueDown adds to its previews.
 *
 * Marked deliberately does not sanitize raw HTML. Preview classes, heading IDs, local asset
 * paths, and data attributes are presentation metadata rather than executable content, so they
 * remain while DOMPurify removes scripts, event handlers, and unsafe URL schemes.
 */
export function sanitizePreviewHtml(html: string): string {
    return DOMPurify.sanitize(html, {
        USE_PROFILES: { html: true },
        ALLOW_DATA_ATTR: true,
    });
}

/** Replace a preview host's contents through the one author-controlled HTML boundary. */
export function mountPreviewHtml(host: Element, html: string): void {
    host.innerHTML = sanitizePreviewHtml(html);
}
