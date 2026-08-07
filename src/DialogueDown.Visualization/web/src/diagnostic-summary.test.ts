import { describe, it, expect, vi } from "vitest";
import { createDiagnosticSummary } from "./diagnostic-summary";

describe("createDiagnosticSummary", () => {
    it("shows each severity total", () => {
        const summary = createDiagnosticSummary(vi.fn());

        summary.setCounts({ error: 2, warning: 1, info: 5 });

        const values = [...summary.element.querySelectorAll(".diagnostic-value")].map(
            (el) => el.textContent,
        );
        expect(values).toEqual(["2", "1", "5"]);
    });

    it("stays visible at zero rather than disappearing", () => {
        const summary = createDiagnosticSummary(vi.fn());

        summary.setCounts({ error: 0, warning: 0, info: 0 });

        // A control that vanishes when clean forces the reader to interpret its absence.
        expect(summary.element.hidden).toBe(false);
        expect(summary.element.classList.contains("clean")).toBe(true);
    });

    it("drops the clean marker once anything is reported", () => {
        const summary = createDiagnosticSummary(vi.fn());
        summary.setCounts({ error: 0, warning: 0, info: 0 });

        summary.setCounts({ error: 0, warning: 1, info: 0 });

        expect(summary.element.classList.contains("clean")).toBe(false);
    });

    it("describes the totals for assistive technology", () => {
        const summary = createDiagnosticSummary(vi.fn());

        summary.setCounts({ error: 1, warning: 0, info: 3 });

        // The icons carry the meaning visually; the label has to carry it otherwise.
        expect(summary.element.getAttribute("aria-label")).toBe(
            "1 errors, 0 warnings, 3 infos — open the Problems panel",
        );
    });

    it("opens the panel when pressed", () => {
        const onOpen = vi.fn();
        const summary = createDiagnosticSummary(onOpen);

        summary.element.click();

        expect(onOpen).toHaveBeenCalledOnce();
    });

    it("reflects whether the panel is open", () => {
        const summary = createDiagnosticSummary(vi.fn());
        expect(summary.element.getAttribute("aria-expanded")).toBe("false");

        summary.setOpen(true);

        expect(summary.element.getAttribute("aria-expanded")).toBe("true");
    });
});
