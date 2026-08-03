import { describe, it, expect, beforeEach } from "vitest";
import { setHelp } from "./help";

describe("setHelp", () => {
    beforeEach(() => {
        document.body.innerHTML = `
      <summary id="help-summary"></summary>
      <div id="help-content"></div>`;
    });

    it("shows source-pane help on the Source tab", () => {
        setHelp("source");
        expect(document.getElementById("help-summary")!.textContent).toContain("Source");
        const html = document.getElementById("help-content")!.innerHTML;
        expect(html).toContain("preview");
        expect(html).toContain("commands, value queries");
        expect(html).toContain("Jump ligature");
        expect(html).toContain("Quote a block"); // the quote/unquote guidance
        expect(html).not.toContain("Arrow keys"); // graph-only guidance is absent
    });

    it("shows graph help on a graph tab", () => {
        setHelp("graph");
        const html = document.getElementById("help-content")!.innerHTML;
        expect(html).toContain("Arrow keys");
        expect(html).toContain("collapse or expand");
    });

    it("shows Explorer help on the empty state", () => {
        setHelp("explorer");
        expect(document.getElementById("help-summary")!.textContent).toContain("Explorer");
        const html = document.getElementById("help-content")!.innerHTML;
        expect(html).toContain("New file");
        expect(html).toContain("Rename");
    });

    it("swaps content when the context changes", () => {
        setHelp("graph");
        setHelp("source");
        expect(document.getElementById("help-content")!.innerHTML).toContain(
            "live Markdown preview",
        );
        expect(document.getElementById("help-content")!.innerHTML).not.toContain("Arrow keys");
    });
});
