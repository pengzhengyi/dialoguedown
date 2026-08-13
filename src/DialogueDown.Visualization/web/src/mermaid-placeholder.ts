/** A per-page capability token that author-controlled raw HTML cannot predict. */
export const MERMAID_PLACEHOLDER_TOKEN = globalThis.crypto.randomUUID();

/** The private data attribute genuine Marked Mermaid placeholders carry. */
export const MERMAID_PLACEHOLDER_ATTRIBUTE = "data-dd-mermaid";
