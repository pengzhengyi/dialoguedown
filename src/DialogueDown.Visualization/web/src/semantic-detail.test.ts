import { describe, it, expect, beforeEach } from "vitest";
import { createNodeDetailPanel } from "./semantic-detail";
import type { DisplayNode } from "./model";

function node(overrides: Partial<DisplayNode> = {}): DisplayNode {
    return { id: "n1", label: "The Market", attributes: [], category: "structure", ...overrides };
}

describe("createNodeDetailPanel", () => {
    beforeEach(() => {
        document.body.innerHTML = "";
    });

    it("starts with a placeholder and a 'Node details' header", () => {
        const panel = createNodeDetailPanel();
        expect(panel.element.querySelector(".table-panel-title")?.textContent).toBe("Node details");
        expect(panel.element.querySelector(".node-detail-body")?.textContent).toContain(
            "Click any node",
        );
    });

    it("shows a node's label, attributes, source, and a rendered preview", () => {
        const panel = createNodeDetailPanel();
        panel.show(
            node({
                label: "The Market",
                attributes: [{ name: "anchor", value: "#the-market" }],
                source: "# The Market",
            }),
        );

        const body = panel.element.querySelector(".node-detail-body")!;
        expect(body.querySelector(".node-detail-heading")?.textContent).toContain("The Market");
        expect(body.textContent).toContain("#the-market"); // an attribute
        expect(body.querySelector("pre code")?.textContent).toBe("# The Market"); // the source
        expect(body.querySelector(".preview")).not.toBeNull(); // a rendered preview
    });

    it("notes that a synthetic node has no source", () => {
        const panel = createNodeDetailPanel();
        panel.show(node({ label: "Speaker (default)", source: undefined }));

        expect(panel.element.querySelector(".inserted-note")?.textContent).toContain(
            "Inserted by the compiler",
        );
    });

    it("auto-expands when a node is selected while collapsed", () => {
        const panel = createNodeDetailPanel();
        const toggle = panel.element.querySelector<HTMLButtonElement>(".table-panel-toggle")!;
        toggle.click(); // collapse it
        expect(panel.element.classList.contains("collapsed")).toBe(true);

        panel.show(node());

        expect(panel.element.classList.contains("collapsed")).toBe(false); // revealed
    });

    it("clears back to the placeholder", () => {
        const panel = createNodeDetailPanel();
        panel.show(node({ source: "x" }));
        panel.clear();

        expect(panel.element.querySelector(".node-detail-body")?.textContent).toContain(
            "Click any node",
        );
    });

    it("collapses and reopens from the caret toggle", () => {
        const panel = createNodeDetailPanel();
        const toggle = panel.element.querySelector<HTMLButtonElement>(".table-panel-toggle")!;
        expect(toggle.getAttribute("aria-expanded")).toBe("true");

        toggle.click();
        expect(panel.element.classList.contains("collapsed")).toBe(true);
        expect(toggle.getAttribute("aria-expanded")).toBe("false");

        toggle.click();
        expect(panel.element.classList.contains("collapsed")).toBe(false);
    });
});
