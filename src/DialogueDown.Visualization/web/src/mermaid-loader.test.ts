import { describe, expect, it, afterEach } from "vitest";
import { loadMermaidFrom } from "./mermaid-loader";

afterEach(() => {
    delete (globalThis as { mermaid?: unknown }).mermaid;
    document.head.querySelectorAll("script[data-dd-mermaid-src]").forEach((tag) => tag.remove());
});

describe("loadMermaidFrom", () => {
    it("uses the copy an exported report already carries, without asking for a URL", async () => {
        const inlined = {
            initialize() {},
            parse: async () => false,
            render: async () => ({ svg: "" }),
        };
        (globalThis as { mermaid?: unknown }).mermaid = inlined;

        await expect(loadMermaidFrom(null)).resolves.toBe(inlined);
    });

    it("fetches the served copy once and reuses it", async () => {
        const load = loadMermaidFrom("/assets/mermaid.abc.js");
        const tag = document.head.querySelector<HTMLScriptElement>("script[data-dd-mermaid-src]");

        expect(tag?.src).toContain("/assets/mermaid.abc.js");
        // The script sets the global as it runs; standing in for that here.
        (globalThis as { mermaid?: unknown }).mermaid = { marker: true };
        tag?.dispatchEvent(new Event("load"));

        await expect(load).resolves.toEqual({ marker: true });
    });

    it("refuses when the report carries no copy and names no source", async () => {
        // An exported report for a script with no diagram inlines nothing, so a fence that
        // appears later must fail loudly rather than hang.
        await expect(loadMermaidFrom(null)).rejects.toThrow(/mermaid/i);
    });

    it("reports a source that will not load rather than hanging", async () => {
        const load = loadMermaidFrom("/assets/missing.js");
        const tag = document.head.querySelector<HTMLScriptElement>("script[data-dd-mermaid-src]");
        tag?.dispatchEvent(new Event("error"));

        await expect(load).rejects.toThrow(/mermaid/i);
    });
});
