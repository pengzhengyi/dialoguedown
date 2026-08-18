import { describe, expect, it, beforeEach, afterEach } from "vitest";
import { hideArrivalNote, showArrivalNote } from "./arrival-note";

describe("the arrival note", () => {
    let anchor: HTMLElement;

    beforeEach(() => {
        document.body.innerHTML = "";
        anchor = document.createElement("div");
        document.body.appendChild(anchor);
    });

    afterEach(() => hideArrivalNote());

    it("says its piece beside the thing the reader landed on", () => {
        showArrivalNote(anchor, "That text is ignored, so no node was made from it.");

        const note = document.querySelector(".dd-arrival-note");
        expect(note?.textContent).toContain("ignored");
    });

    it("replaces a previous note rather than stacking a second one", () => {
        showArrivalNote(anchor, "first");
        showArrivalNote(anchor, "second");

        expect(document.querySelectorAll(".dd-arrival-note")).toHaveLength(1);
        expect(document.querySelector(".dd-arrival-note")?.textContent).toContain("second");
    });

    it("goes away when there is nothing to say", () => {
        showArrivalNote(anchor, "gone in a moment");
        hideArrivalNote();

        expect(document.querySelector(".dd-arrival-note")).toBeNull();
    });

    it("is announced, since a reader watching the graph may not look at it", () => {
        showArrivalNote(anchor, "announced");

        const live = document.querySelector(".dd-arrival-note");
        expect(live?.getAttribute("role")).toBe("status");
    });
});
