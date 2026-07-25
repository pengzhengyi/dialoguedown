import { describe, it, expect, vi, afterEach } from "vitest";
import { createConfigView, type ConfigViewOptions } from "./config-view";
import type { ConfigReport } from "./model";

/** The Config tab element (its handle's element), for DOM assertions. */
function mount(config: ConfigReport, options: ConfigViewOptions = {}): HTMLElement {
    return createConfigView(config, options).element;
}

/** A configured-from-file report with one speaker carrying a custom and a reserved tag. */
function withFile(): ConfigReport {
    return {
        file: { path: "/proj/dialogue.toml", source: '[[speakers]]\nname = "Alice"\n' },
        speakers: [
            {
                name: "Alice",
                id: "A",
                tags: [
                    { name: "role", value: "guide", reserved: false },
                    { name: "default", reserved: true },
                ],
            },
        ],
    };
}

describe("createConfigView", () => {
    it("shows the TOML source in a read-only editor and the speakers table", () => {
        const view = mount(withFile());

        const editor = view.querySelector(".config-source .cm-editor");
        expect(editor).not.toBeNull();
        expect(view.querySelector(".config-source")?.textContent).toContain("Alice");

        const rows = view.querySelectorAll(".config-speakers-table tbody tr");
        expect(rows).toHaveLength(1);
        expect(rows[0].textContent).toContain("Alice");
    });

    it("shows the id with its @ sigil, exactly as a script references it", () => {
        const view = mount(withFile());

        const idCell = view.querySelector(".config-speakers-table tbody td:nth-child(2)");
        expect(idCell?.textContent).toBe("@A");
    });

    it("colors reserved and custom tags apart with distinct chip classes", () => {
        const view = mount(withFile());

        const custom = view.querySelector(".config-tag-custom");
        const reserved = view.querySelector(".config-tag-reserved");
        expect(custom?.textContent).toBe("#role=guide");
        expect(reserved?.textContent).toBe("##default");
    });

    it("shows the configured compilation mode above the speakers", () => {
        const view = mount({ ...withFile(), mode: "best-effort" });

        expect(view.querySelector(".config-mode-value")?.textContent).toBe("best-effort");
    });

    it("defaults the mode label to stage-boundary when unset", () => {
        const view = mount(withFile());

        expect(view.querySelector(".config-mode-value")?.textContent).toBe("stage-boundary");
    });

    it("shows the mode row even when there is no config file", () => {
        const view = mount({ speakers: [], mode: "best-effort" });

        expect(view.querySelector(".config-mode-value")?.textContent).toBe("best-effort");
    });

    it("shows a friendly explanation and no editor when there is no config file", () => {
        const view = mount({ speakers: [] });

        expect(view.querySelector(".config-source .cm-editor")).toBeNull();
        expect(view.querySelector(".config-empty-state")?.textContent).toContain("dialogue.toml");
        expect(view.querySelector(".config-side .config-empty")?.textContent).toContain(
            "No configured speakers",
        );
    });

    describe("no-config create call to action", () => {
        const noFile = (): ConfigReport => ({ speakers: [] });

        it("shows the create button in Edit and the hint in View", () => {
            const edit = mount(noFile(), {
                editable: true,
                onCreateConfig: () => Promise.resolve(),
            });
            expect(edit.querySelector<HTMLButtonElement>(".config-create-button")?.hidden).toBe(
                false,
            );
            expect(edit.querySelector<HTMLElement>(".config-create-hint")?.hidden).toBe(true);

            const view = mount(noFile(), {
                editable: false,
                onCreateConfig: () => Promise.resolve(),
            });
            expect(view.querySelector<HTMLButtonElement>(".config-create-button")?.hidden).toBe(
                true,
            );
            expect(view.querySelector<HTMLElement>(".config-create-hint")?.hidden).toBe(false);
        });

        it("shows neither button nor hint in the static export (no create handler)", () => {
            const view = mount(noFile(), { editable: false });

            expect(view.querySelector(".config-create-button")).toBeNull();
            expect(view.querySelector(".config-create-hint")).toBeNull();
        });

        it("flips the call to action with setEditable", () => {
            const handle = createConfigView(noFile(), {
                editable: false,
                onCreateConfig: () => Promise.resolve(),
            });
            const button = handle.element.querySelector<HTMLButtonElement>(".config-create-button");

            expect(button?.hidden).toBe(true);
            handle.setEditable(true);
            expect(button?.hidden).toBe(false);
        });

        it("invokes the create handler on click", () => {
            const onCreateConfig = vi.fn(() => Promise.resolve());
            const view = mount(noFile(), { editable: true, onCreateConfig });

            view.querySelector<HTMLButtonElement>(".config-create-button")!.click();

            expect(onCreateConfig).toHaveBeenCalledOnce();
        });

        it("shows the error and re-enables the button when the create fails", async () => {
            const onCreateConfig = vi.fn(() =>
                Promise.reject(new Error("A dialogue.toml already exists.")),
            );
            const view = mount(noFile(), { editable: true, onCreateConfig });
            const button = view.querySelector<HTMLButtonElement>(".config-create-button")!;

            button.click();
            expect(button.disabled).toBe(true); // busy while creating

            await new Promise((resolve) => setTimeout(resolve)); // let the rejection settle
            const error = view.querySelector<HTMLElement>(".config-create-error")!;
            expect(error.hidden).toBe(false);
            expect(error.textContent).toContain("already exists");
            expect(button.disabled).toBe(false); // re-enabled to retry
        });
    });

    it("shows an empty-speakers note when a file has no configured speakers", () => {
        const view = mount({
            file: { path: "/p/dialogue.toml", source: "# empty\n" },
            speakers: [],
        });

        expect(view.querySelector(".config-source .cm-editor")).not.toBeNull();
        expect(view.querySelector(".config-side .config-empty")?.textContent).toContain(
            "No configured speakers",
        );
    });

    it("hides and reopens the speakers panel from the divider toggle", () => {
        const view = mount(withFile());

        const toggle = view.querySelector<HTMLButtonElement>(".config-divider .collapse-toggle");
        expect(toggle).not.toBeNull();
        expect(view.classList.contains("config-collapsed")).toBe(false);

        toggle!.click();
        expect(view.classList.contains("config-collapsed")).toBe(true);

        toggle!.click();
        expect(view.classList.contains("config-collapsed")).toBe(false);
    });

    describe("click to copy", () => {
        const writeText = vi.fn().mockResolvedValue(undefined);
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
        afterEach(() => writeText.mockClear());

        function clickCopy(selector: string): void {
            const view = mount(withFile());
            view.querySelector<HTMLElement>(selector)!.dispatchEvent(
                new MouseEvent("click", { bubbles: true }),
            );
        }

        it("copies a speaker's name", () => {
            clickCopy(".config-speakers-table tbody td:nth-child(1)");
            expect(writeText).toHaveBeenCalledWith("Alice");
        });

        it("copies the @id", () => {
            clickCopy(".config-speakers-table tbody td:nth-child(2)");
            expect(writeText).toHaveBeenCalledWith("@A");
        });

        it("copies a tag chip's text", () => {
            clickCopy(".config-tag-reserved");
            expect(writeText).toHaveBeenCalledWith("##default");
        });
    });

    describe("editable mode", () => {
        it("is editable (no read-only aria) and forwards edits when editable", () => {
            const edits: string[] = [];
            const handle = createConfigView(withFile(), {
                editable: true,
                onChange: (source) => edits.push(source),
            });
            const content = handle.element.querySelector<HTMLElement>(
                ".config-source .cm-content",
            )!;
            expect(content.getAttribute("aria-readonly")).not.toBe("true");
            expect(content.getAttribute("aria-label")).toBe("Configuration source editor");

            handle.setContent('name = "typed"');
            expect(edits).toContain('name = "typed"');
        });

        it("reconfigures between editable and read-only in place", () => {
            const handle = createConfigView(withFile(), { editable: false });
            const content = handle.element.querySelector<HTMLElement>(
                ".config-source .cm-content",
            )!;
            expect(content.getAttribute("aria-readonly")).toBe("true");

            handle.setEditable(true);
            expect(content.getAttribute("aria-readonly")).not.toBe("true");

            handle.setEditable(false);
            expect(content.getAttribute("aria-readonly")).toBe("true");
        });

        it("setContent replaces the editor text", () => {
            const handle = createConfigView(withFile(), { editable: true });
            handle.setContent('[[speakers]]\nname = "Zoe"\n');
            expect(handle.element.querySelector(".config-source")?.textContent).toContain("Zoe");
        });

        it("updateConfig re-renders the speakers and the mode from a fresh report", () => {
            const handle = createConfigView({ ...withFile(), mode: "stage-boundary" });
            handle.updateConfig({
                file: { path: "/p/dialogue.toml", source: "" },
                mode: "best-effort",
                speakers: [{ name: "Zoe", id: "Z", tags: [] }],
            });
            const table = handle.element.querySelector(".config-speakers-table");
            expect(table?.querySelectorAll("tbody tr")).toHaveLength(1);
            expect(table?.textContent).toContain("Zoe");
            expect(table?.textContent).not.toContain("Alice");
            expect(handle.element.querySelector(".config-mode-value")?.textContent).toBe(
                "best-effort",
            );
        });

        it("setStale toggles the unsaved-speakers hint", () => {
            const handle = createConfigView(withFile());
            const hint = handle.element.querySelector<HTMLElement>(".config-stale-hint")!;
            expect(hint.hidden).toBe(true);
            handle.setStale(true);
            expect(hint.hidden).toBe(false);
            handle.setStale(false);
            expect(hint.hidden).toBe(true);
        });

        it("wires editor affordances: a fold gutter and search", () => {
            const view = mount(withFile());
            expect(view.querySelector(".config-source .cm-foldGutter")).not.toBeNull();
        });
    });
});
