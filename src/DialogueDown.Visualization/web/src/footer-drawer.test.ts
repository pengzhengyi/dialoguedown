import { describe, it, expect, beforeEach } from "vitest";
import { createFooterDrawer } from "./footer-drawer";

/** The footer markup the drawer attaches to, mirroring index.html. */
function mountFooter(): { footer: HTMLElement; host: HTMLElement } {
    document.body.innerHTML = `
        <footer class="app-footer">
            <div class="status-line"></div>
            <div id="footer-drawer" hidden></div>
        </footer>`;
    return {
        footer: document.querySelector(".app-footer")!,
        host: document.getElementById("footer-drawer")!,
    };
}

function panelBody(text: string): HTMLElement {
    const el = document.createElement("div");
    el.textContent = text;
    return el;
}

describe("createFooterDrawer", () => {
    let host: HTMLElement;

    beforeEach(() => {
        host = mountFooter().host;
    });

    function drawer() {
        return createFooterDrawer({
            host,
            panels: [
                { id: "problems", label: "Problems", body: panelBody("the problems") },
                { id: "help", label: "Help", body: panelBody("the help") },
            ],
        });
    }

    it("starts closed, so the stage keeps the whole window until asked", () => {
        const d = drawer();

        expect(d.isOpen()).toBe(false);
        expect(host.hidden).toBe(true);
    });

    it("opens on the panel it was asked for", () => {
        const d = drawer();

        d.open("help");

        expect(d.isOpen()).toBe(true);
        expect(host.hidden).toBe(false);
        expect(d.activePanel()).toBe("help");
        expect(host.textContent).toContain("the help");
    });

    it("switches panels from the tab bar without closing", () => {
        const d = drawer();
        d.open("problems");

        const helpTab = host.querySelector<HTMLElement>('[data-panel="help"]')!;
        helpTab.click();

        expect(d.activePanel()).toBe("help");
        expect(d.isOpen()).toBe(true);
        // Only the active panel is exposed, so a screen reader is not read both at once.
        expect(host.querySelector<HTMLElement>('[data-body="problems"]')!.hidden).toBe(true);
        expect(host.querySelector<HTMLElement>('[data-body="help"]')!.hidden).toBe(false);
    });

    it("marks the active tab for assistive technology", () => {
        const d = drawer();

        d.open("problems");

        const tabs = host.querySelectorAll('[role="tab"]');
        expect(tabs).toHaveLength(2);
        expect(host.querySelector('[data-panel="problems"]')!.getAttribute("aria-selected")).toBe(
            "true",
        );
        expect(host.querySelector('[data-panel="help"]')!.getAttribute("aria-selected")).toBe(
            "false",
        );
    });

    it("opening the panel already shown closes the drawer, so one control toggles", () => {
        const d = drawer();
        d.open("problems");

        d.open("problems");

        expect(d.isOpen()).toBe(false);
    });

    it("opening a different panel while open switches rather than closing", () => {
        const d = drawer();
        d.open("problems");

        d.open("help");

        expect(d.isOpen()).toBe(true);
        expect(d.activePanel()).toBe("help");
    });

    it("returns focus to whatever opened it when closed", () => {
        const opener = document.createElement("button");
        document.body.appendChild(opener);
        const d = drawer();
        d.open("problems", opener);

        d.close();

        // The control that opened the drawer is where the reader expects to land, and it is
        // how they reopen it.
        expect(document.activeElement).toBe(opener);
    });

    it("closes from its own close button", () => {
        const d = drawer();
        d.open("problems");

        host.querySelector<HTMLElement>(".drawer-close")!.click();

        expect(d.isOpen()).toBe(false);
    });

    it("notifies when the open state changes, so a toggle can reflect it", () => {
        const seen: boolean[] = [];
        const d = createFooterDrawer({
            host,
            panels: [{ id: "problems", label: "Problems", body: panelBody("x") }],
            onToggle: (open) => seen.push(open),
        });

        d.open("problems");
        d.close();

        expect(seen).toEqual([true, false]);
    });
});
