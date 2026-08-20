import type { MermaidApi } from "./mermaid-preview";

/** Where the page says its Mermaid build lives, or null when it carries one already. */
export function mermaidSource(): string | null {
    const declared = (globalThis as { __DD_MERMAID__?: string }).__DD_MERMAID__;
    return typeof declared === "string" && declared.length > 0 ? declared : null;
}

/**
 * Mermaid is the largest thing the report can draw with and the rarest thing it needs, so it is
 * never part of the client. A served report fetches it from `source` the first time a script shows
 * a diagram; an exported report for a script that has one carries it already, and sets the global
 * before this runs. An export for a script with no diagram carries neither, which is why a missing
 * source is an error rather than a wait.
 */
export async function loadMermaidFrom(source: string | null): Promise<MermaidApi> {
    const existing = (globalThis as { mermaid?: MermaidApi }).mermaid;
    if (existing) return existing;
    if (source === null) {
        throw new Error("This report carries no Mermaid build, so it cannot draw a diagram.");
    }

    await new Promise<void>((resolve, reject) => {
        const tag = document.createElement("script");
        tag.dataset.ddMermaidSrc = "";
        tag.addEventListener("load", () => resolve());
        tag.addEventListener("error", () =>
            reject(new Error(`Mermaid could not be loaded from ${source}.`)),
        );
        tag.src = source;
        document.head.appendChild(tag);
    });

    const loaded = (globalThis as { mermaid?: MermaidApi }).mermaid;
    if (!loaded) throw new Error(`Mermaid loaded from ${source} but defined nothing.`);
    return loaded;
}
