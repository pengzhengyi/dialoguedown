import { describe, it, expect, beforeEach } from "vitest";
import { annotateHeadingAnchors, headingJumpLink } from "./heading-anchors";

describe("headingJumpLink", () => {
    it("formats a heading and slug as a Markdown jump link", () => {
        expect(headingJumpLink("The Market", "the-market")).toBe("[The Market](#the-market)");
    });
});

describe("annotateHeadingAnchors", () => {
    let container: HTMLElement;
    beforeEach(() => {
        container = document.createElement("div");
    });

    const link = () => container.querySelector<HTMLElement>(".heading-anchor-link");

    it("adds one link copying the full jump target", () => {
        container.innerHTML = `<h1 id="the-market">The Market</h1>`;

        annotateHeadingAnchors(container);

        // The tooltip (data-copy) is exactly what the affordance copies.
        expect(link()!.dataset.copy).toBe("[The Market](#the-market)");
        expect(link()!.getAttribute("aria-label")).toBe("Copy jump link to The Market");
    });

    it("renders its SVG without polluting the heading's textContent", () => {
        container.innerHTML = `<h1 id="the-market">The Market</h1>`;

        annotateHeadingAnchors(container);

        // The link is an SVG, so it adds no text.
        expect(container.querySelector("h1")!.textContent).toBe("The Market");
    });

    it("skips a heading with an empty slug — it is not a jump target", () => {
        container.innerHTML = `<h1 id="">!!!</h1>`;

        annotateHeadingAnchors(container);

        expect(container.querySelector(".heading-anchor")).toBeNull();
    });

    it("is idempotent, so re-annotating after a preview re-render adds no duplicates", () => {
        container.innerHTML = `<h1 id="the-market">The Market</h1>`;

        annotateHeadingAnchors(container);
        annotateHeadingAnchors(container);

        expect(container.querySelectorAll(".heading-anchor-link")).toHaveLength(1);
    });

    it("uses the heading's plain text as the jump-link label, ignoring inline formatting", () => {
        container.innerHTML = `<h1 id="the-market">The <em>Market</em></h1>`;

        annotateHeadingAnchors(container);

        expect(link()!.dataset.copy).toBe("[The Market](#the-market)");
    });
});
