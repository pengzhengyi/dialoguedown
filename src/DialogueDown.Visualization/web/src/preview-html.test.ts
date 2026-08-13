import { describe, expect, it } from "vitest";
import { mountPreviewHtml, sanitizePreviewHtml } from "./preview-html";

describe("sanitizePreviewHtml", () => {
    it("removes active content from author-controlled Markdown HTML", () => {
        const html =
            '<script>alert(1)</script><img src="x" onerror="alert(2)">' +
            '<a href="javascript:alert(3)">unsafe</a>';

        const sanitized = sanitizePreviewHtml(html);

        expect(sanitized).not.toContain("<script");
        expect(sanitized).not.toContain("onerror");
        expect(sanitized).not.toContain("javascript:");
    });

    it("preserves the metadata and local assets the preview owns", () => {
        const html =
            '<div class="dd-preview-ignored-region" data-mermaid-source="flowchart LR">' +
            '<h2 id="scene">Scene</h2>' +
            '<a href="#scene">jump</a>' +
            '<img src="assets/portrait.png" alt="Portrait">' +
            "</div>";

        const sanitized = sanitizePreviewHtml(html);

        expect(sanitized).toContain('class="dd-preview-ignored-region"');
        expect(sanitized).toContain('data-mermaid-source="flowchart LR"');
        expect(sanitized).toContain('id="scene"');
        expect(sanitized).toContain('href="#scene"');
        expect(sanitized).toContain('src="assets/portrait.png"');
    });
});

describe("mountPreviewHtml", () => {
    it("writes only sanitized HTML into the preview host", () => {
        const host = document.createElement("div");

        mountPreviewHtml(host, '<p onclick="alert(1)">Safe words</p>');

        expect(host.textContent).toBe("Safe words");
        expect(host.querySelector("p")?.hasAttribute("onclick")).toBe(false);
    });
});
