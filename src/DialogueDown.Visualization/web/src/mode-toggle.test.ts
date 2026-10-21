import { describe, it, expect, vi } from "vitest";
import { createModeToggle } from "./mode-toggle";

describe("createModeToggle", () => {
    it("renders View and Edit options and reflects the initial mode", () => {
        const toggle = createModeToggle("view", vi.fn());
        const modes = [...toggle.element.querySelectorAll(".mode-toggle-option")].map((b) =>
            b.getAttribute("data-mode"),
        );

        expect(modes).toEqual(["view", "edit"]);
        expect(pressed(toggle.element, "view")).toBe("true");
        expect(pressed(toggle.element, "edit")).toBe("false");
    });

    it("calls onSelect with the clicked mode", () => {
        const onSelect = vi.fn();
        const toggle = createModeToggle("view", onSelect);

        button(toggle.element, "edit").click();

        expect(onSelect).toHaveBeenCalledWith("edit");
    });

    it("reflect moves the pressed state to the given mode", () => {
        const toggle = createModeToggle("view", vi.fn());

        toggle.reflect("edit");

        expect(pressed(toggle.element, "edit")).toBe("true");
        expect(pressed(toggle.element, "view")).toBe("false");
    });

    it("starts enabled", () => {
        const toggle = createModeToggle("view", vi.fn());

        expect(toggle.element.getAttribute("aria-disabled")).toBe("false");
        expect(button(toggle.element, "edit").disabled).toBe(false);
    });

    it("disabling makes both options inert and says why on the group", () => {
        const onSelect = vi.fn();
        const toggle = createModeToggle("edit", onSelect);

        toggle.setEnabled(false, "The Playbook is compiled output.");
        button(toggle.element, "view").click();

        expect(toggle.element.getAttribute("aria-disabled")).toBe("true");
        expect(button(toggle.element, "view").disabled).toBe(true);
        expect(button(toggle.element, "edit").disabled).toBe(true);
        // On the group, because a disabled button shows no tooltip.
        expect(toggle.element.title).toBe("The Playbook is compiled output.");
        expect(onSelect).not.toHaveBeenCalled();
    });

    it("keeps the pressed mode visible while disabled", () => {
        const toggle = createModeToggle("edit", vi.fn());

        toggle.setEnabled(false, "Read-only here.");

        expect(pressed(toggle.element, "edit")).toBe("true");
    });

    it("re-enabling restores the options and clears the explanation", () => {
        const onSelect = vi.fn();
        const toggle = createModeToggle("view", onSelect);
        toggle.setEnabled(false, "Read-only here.");

        toggle.setEnabled(true);
        button(toggle.element, "edit").click();

        expect(toggle.element.getAttribute("aria-disabled")).toBe("false");
        expect(toggle.element.title).toBe("");
        expect(onSelect).toHaveBeenCalledWith("edit");
    });
});

function button(root: HTMLElement, mode: string): HTMLButtonElement {
    return root.querySelector(`[data-mode="${mode}"]`)!;
}

function pressed(root: HTMLElement, mode: string): string | null {
    return button(root, mode).getAttribute("aria-pressed");
}
