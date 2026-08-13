import { describe, expect, it, vi } from "vitest";
import { createMermaidPreviewService, type MermaidApi } from "./mermaid-preview";
import { mountPreviewHtml } from "./preview-html";
import { renderMarkdown } from "./text";

function host(source: string): HTMLElement {
    const element = document.createElement("div");
    mountPreviewHtml(element, renderMarkdown(`\`\`\`mermaid\n${source}\n\`\`\``));
    document.body.appendChild(element);
    return element;
}

function fakeMermaid(overrides: Partial<MermaidApi> = {}): MermaidApi {
    return {
        initialize: vi.fn(),
        parse: vi.fn().mockResolvedValue({ diagramType: "flowchart-v2" }),
        render: vi.fn().mockResolvedValue({ svg: "<svg></svg>" }),
        ...overrides,
    };
}

describe("createMermaidPreviewService", () => {
    it("renders valid source through strict Mermaid configuration", async () => {
        const api = fakeMermaid();
        const service = createMermaidPreviewService({
            load: async () => api,
            theme: () => "dark",
        });
        const preview = host("flowchart LR\nA --> B");

        await service.renderNow(preview);

        expect(api.initialize).toHaveBeenCalledWith(
            expect.objectContaining({
                startOnLoad: false,
                securityLevel: "strict",
                maxTextSize: 50_000,
                theme: "dark",
            }),
        );
        expect(api.render).toHaveBeenCalledWith(
            expect.stringMatching(/^dd-mermaid-\d+$/),
            "flowchart LR\nA --> B",
        );
        expect(preview.querySelector(".mermaid-diagram svg")).not.toBeNull();
    });

    it("keeps invalid source visible with one local message", async () => {
        const api = fakeMermaid({ parse: vi.fn().mockResolvedValue(false) });
        const service = createMermaidPreviewService({ load: async () => api });
        const preview = host("not a diagram");

        await service.renderNow(preview);

        expect(preview.querySelector(".mermaid-source")?.textContent).toBe("not a diagram");
        expect(preview.querySelectorAll(".mermaid-error")).toHaveLength(1);
        expect(api.render).not.toHaveBeenCalled();
    });

    it("gives every mounted diagram a unique render id", async () => {
        const api = fakeMermaid();
        const service = createMermaidPreviewService({ load: async () => api });
        const preview = document.createElement("div");
        mountPreviewHtml(
            preview,
            renderMarkdown(
                "```mermaid\nflowchart LR\nA --> B\n```\n\n```mermaid\nflowchart LR\nC --> D\n```",
            ),
        );
        document.body.appendChild(preview);

        await service.renderNow(preview);

        const ids = vi.mocked(api.render).mock.calls.map(([id]) => id);
        expect(new Set(ids).size).toBe(2);
    });

    it("adds a fallback accessible name when Mermaid provides none", async () => {
        const service = createMermaidPreviewService({ load: async () => fakeMermaid() });
        const preview = host("flowchart LR\nA --> B");

        await service.renderNow(preview);

        const diagram = preview.querySelector(".mermaid-diagram");
        expect(diagram?.getAttribute("role")).toBe("img");
        expect(diagram?.getAttribute("aria-label")).toBe("Mermaid diagram");
    });

    it("does not overwrite a newer host revision with stale SVG", async () => {
        let resolveFirst: ((value: { svg: string }) => void) | undefined;
        const first = new Promise<{ svg: string }>((resolve) => (resolveFirst = resolve));
        const api = fakeMermaid({
            render: vi
                .fn()
                .mockReturnValueOnce(first)
                .mockResolvedValueOnce({ svg: '<svg data-version="new"></svg>' }),
        });
        const service = createMermaidPreviewService({ load: async () => api });
        const preview = host("flowchart LR\nold --> result");
        const oldRender = service.renderNow(preview);
        await vi.waitFor(() => expect(api.render).toHaveBeenCalledTimes(1));

        mountPreviewHtml(preview, renderMarkdown("```mermaid\nflowchart LR\nnew --> result\n```"));
        const newRender = service.renderNow(preview);
        resolveFirst?.({ svg: '<svg data-version="old"></svg>' });
        await Promise.all([oldRender, newRender]);

        expect(preview.querySelector('svg[data-version="old"]')).toBeNull();
        expect(preview.querySelector('svg[data-version="new"]')).not.toBeNull();
    });

    it("serializes Mermaid calls because its configuration is global", async () => {
        let active = 0;
        let mostActive = 0;
        const api = fakeMermaid({
            render: vi.fn().mockImplementation(async () => {
                active++;
                mostActive = Math.max(mostActive, active);
                await Promise.resolve();
                active--;
                return { svg: "<svg></svg>" };
            }),
        });
        const service = createMermaidPreviewService({ load: async () => api });

        await Promise.all([
            service.renderNow(host("flowchart LR\nA --> B")),
            service.renderNow(host("flowchart LR\nC --> D")),
        ]);

        expect(mostActive).toBe(1);
    });
});
