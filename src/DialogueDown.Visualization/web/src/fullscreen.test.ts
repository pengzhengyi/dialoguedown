import { describe, it, expect } from "vitest";
import { initFullscreen, MAXIMIZED_CLASS, ZEN_CLASS } from "./fullscreen";

/** A throwaway document so each controller's keydown listener stays isolated. */
function scratch(): { doc: Document; root: HTMLElement } {
    const doc = document.implementation.createHTMLDocument("fullscreen-test");
    return { doc, root: doc.body };
}

/** Dispatch a bubbling keydown from `target` (defaults to the document body). */
function press(
    doc: Document,
    key: string,
    init: KeyboardEventInit = {},
    target: EventTarget = doc.body,
): KeyboardEvent {
    const event = new KeyboardEvent("keydown", { key, bubbles: true, cancelable: true, ...init });
    target.dispatchEvent(event);
    return event;
}

describe("initFullscreen", () => {
    it("toggles the maximized class on the root", () => {
        const { doc, root } = scratch();
        const fs = initFullscreen(root, doc);
        expect(fs.isMaximized()).toBe(false);
        fs.toggle();
        expect(root.classList.contains(MAXIMIZED_CLASS)).toBe(true);
        expect(fs.isMaximized()).toBe(true);
        fs.toggle();
        expect(root.classList.contains(MAXIMIZED_CLASS)).toBe(false);
    });

    it("exit leaves full screen and is a no-op when already minimized", () => {
        const { doc, root } = scratch();
        const fs = initFullscreen(root, doc);
        fs.exit();
        expect(fs.isMaximized()).toBe(false);
        fs.toggle();
        fs.exit();
        expect(fs.isMaximized()).toBe(false);
    });

    it("toggles on the `f` key and prevents its default", () => {
        const { doc, root } = scratch();
        const fs = initFullscreen(root, doc);
        const event = press(doc, "f");
        expect(fs.isMaximized()).toBe(true);
        expect(event.defaultPrevented).toBe(true);
        press(doc, "F"); // caps/shift still toggles
        expect(fs.isMaximized()).toBe(false);
    });

    it("ignores `f` with a modifier so browser/OS shortcuts are untouched", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);
        for (const mod of [{ ctrlKey: true }, { metaKey: true }, { altKey: true }]) {
            press(doc, "f", mod);
            expect(fs.isMaximized()).toBe(false);
        }
    });

    it("ignores `f` while typing in the editor or a form field", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);

        const editor = doc.createElement("div");
        editor.className = "cm-editor";
        const content = doc.createElement("div");
        editor.appendChild(content);
        doc.body.appendChild(editor);
        press(doc, "f", {}, content);
        expect(fs.isMaximized()).toBe(false);

        const input = doc.createElement("input");
        doc.body.appendChild(input);
        press(doc, "f", {}, input);
        expect(fs.isMaximized()).toBe(false);
    });

    it("exits on Escape only while maximized, and yields otherwise", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);

        const ignored = press(doc, "Escape");
        expect(ignored.defaultPrevented).toBe(false);

        fs.toggle();
        const handled = press(doc, "Escape");
        expect(fs.isMaximized()).toBe(false);
        expect(handled.defaultPrevented).toBe(true);
    });

    it("respects a key another handler already consumed", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);
        const event = new KeyboardEvent("keydown", { key: "f", bubbles: true, cancelable: true });
        event.preventDefault();
        doc.body.dispatchEvent(event);
        expect(fs.isMaximized()).toBe(false);
    });

    it("reflects the pressed state on every maximize button", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);
        const button = doc.createElement("button");
        button.className = "maximize-button";
        doc.body.appendChild(button);

        fs.toggle();
        expect(button.getAttribute("aria-pressed")).toBe("true");
        expect(button.title).toContain("Exit");

        fs.toggle();
        expect(button.getAttribute("aria-pressed")).toBe("false");
        expect(button.title).toContain("Full screen");
    });
});

describe("initFullscreen — Zen mode", () => {
    it("hides the chrome like full screen, and additionally flags Zen", () => {
        const { doc, root } = scratch();
        const fs = initFullscreen(root, doc);

        fs.toggleZen();

        expect(fs.isZen()).toBe(true);
        expect(fs.mode()).toBe("zen");
        // Zen reuses the maximized chrome-hiding rather than restating it.
        expect(root.classList.contains(MAXIMIZED_CLASS)).toBe(true);
        expect(root.classList.contains(ZEN_CLASS)).toBe(true);
    });

    it("toggles back to normal, clearing both flags", () => {
        const { doc, root } = scratch();
        const fs = initFullscreen(root, doc);

        fs.toggleZen();
        fs.toggleZen();

        expect(fs.mode()).toBe("normal");
        expect(root.classList.contains(MAXIMIZED_CLASS)).toBe(false);
        expect(root.classList.contains(ZEN_CLASS)).toBe(false);
    });

    it("deepens full screen into Zen, and `f` from Zen returns to normal", () => {
        const { doc, root } = scratch();
        const fs = initFullscreen(root, doc);

        fs.toggle(); // maximized
        fs.toggleZen(); // deepen
        expect(fs.mode()).toBe("zen");

        fs.toggle(); // stepping out of Zen leaves focus mode entirely
        expect(fs.mode()).toBe("normal");
        expect(root.classList.contains(ZEN_CLASS)).toBe(false);
    });

    it("toggles on the `z` key and prevents its default", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);

        const event = press(doc, "z");
        expect(fs.isZen()).toBe(true);
        expect(event.defaultPrevented).toBe(true);

        press(doc, "Z"); // caps/shift still toggles
        expect(fs.isZen()).toBe(false);
    });

    it("ignores `z` with a modifier so undo is untouched", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);
        for (const mod of [{ ctrlKey: true }, { metaKey: true }, { altKey: true }]) {
            press(doc, "z", mod);
            expect(fs.isZen()).toBe(false);
        }
    });

    it("ignores `z` while typing", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);

        const editor = doc.createElement("div");
        editor.className = "cm-editor";
        const content = doc.createElement("div");
        editor.appendChild(content);
        doc.body.appendChild(editor);

        press(doc, "z", {}, content);
        expect(fs.isZen()).toBe(false);
    });

    it("exits Zen on Escape", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);

        fs.toggleZen();
        const handled = press(doc, "Escape");

        expect(fs.mode()).toBe("normal");
        expect(handled.defaultPrevented).toBe(true);
    });

    it("exit() clears Zen too, so the corner chip leaves either mode", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);

        fs.toggleZen();
        fs.exit();

        expect(fs.mode()).toBe("normal");
    });

    // Focus needs a real browsing context, which the scratch document does not have, so
    // these two run against the live document and clean up after themselves.
    it("blurs a control inside a panel Zen hides, so it cannot be activated unseen", () => {
        const fs = initFullscreen(document.body, document);

        // The preview's collapse toggle lives inside the divider Zen hides. If focus stayed
        // there, Enter would collapse the preview and persist that choice — the one thing
        // Zen promises never to touch.
        const divider = document.createElement("div");
        divider.className = "source-divider";
        const toggle = document.createElement("button");
        divider.appendChild(toggle);
        document.body.appendChild(divider);
        toggle.focus();
        expect(document.activeElement).toBe(toggle);

        try {
            fs.toggleZen();
            expect(document.activeElement).not.toBe(toggle);
        } finally {
            fs.exit();
            divider.remove();
        }
    });

    it("blurs the ignored-content command with the Preview shell Zen hides", () => {
        const fs = initFullscreen(document.body, document);
        const shell = document.createElement("div");
        shell.className = "source-preview-shell";
        const toggle = document.createElement("button");
        toggle.className = "dd-ignored-preview-command";
        shell.appendChild(toggle);
        document.body.appendChild(shell);
        toggle.focus();

        try {
            fs.toggleZen();
            expect(document.activeElement).not.toBe(toggle);
        } finally {
            fs.exit();
            shell.remove();
        }
    });

    it("reflects Zen on the Zen button, and only for Zen", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);
        const button = doc.createElement("button");
        button.className = "zen-button";
        doc.body.appendChild(button);

        // Plain full screen leaves Zen available, not engaged.
        fs.toggle();
        expect(button.getAttribute("aria-pressed")).toBe("false");
        expect(button.title).toContain("Zen mode (z)");

        fs.toggleZen();
        expect(button.getAttribute("aria-pressed")).toBe("true");
        expect(button.title).toContain("Exit Zen mode");

        fs.exit();
        expect(button.getAttribute("aria-pressed")).toBe("false");
    });

    it("blurs a control in the chrome, which both focus modes hide", () => {
        const fs = initFullscreen(document.body, document);

        // Save/Discard/Reload and the help toggle live in the chrome, so full screen alone
        // can already strand focus on them.
        const footer = document.createElement("div");
        footer.className = "app-footer";
        const save = document.createElement("button");
        footer.appendChild(save);
        document.body.appendChild(footer);
        save.focus();

        try {
            fs.toggle(); // full screen, not Zen
            expect(document.activeElement).not.toBe(save);
        } finally {
            fs.exit();
            footer.remove();
        }
    });

    it("full screen leaves a panel control focused, because it stays visible", () => {
        const fs = initFullscreen(document.body, document);

        // Only Zen hides the panels; blurring here would steal focus from something the
        // reader can still see and use.
        const divider = document.createElement("div");
        divider.className = "source-divider";
        const toggle = document.createElement("button");
        divider.appendChild(toggle);
        document.body.appendChild(divider);
        toggle.focus();

        try {
            fs.toggle(); // full screen
            expect(document.activeElement).toBe(toggle);
        } finally {
            fs.exit();
            divider.remove();
        }
    });

    it("leaves focus alone when it is outside the panels Zen hides", () => {
        const fs = initFullscreen(document.body, document);

        const editor = document.createElement("button");
        document.body.appendChild(editor);
        editor.focus();

        try {
            fs.toggleZen();
            expect(document.activeElement).toBe(editor);
        } finally {
            fs.exit();
            editor.remove();
        }
    });

    it("labels the maximize buttons for Zen so the way out is discoverable", () => {
        const { doc } = scratch();
        const fs = initFullscreen(doc.body, doc);
        const button = doc.createElement("button");
        button.className = "maximize-button";
        doc.body.appendChild(button);

        fs.toggleZen();

        expect(button.getAttribute("aria-pressed")).toBe("true");
        expect(button.title).toContain("Exit");
    });
});
