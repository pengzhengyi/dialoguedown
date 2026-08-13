import { afterEach, describe, expect, it } from "vitest";
import { createIgnoredPreviewController, type IgnoredPreviewController } from "./ignored-preview";

let controllers: IgnoredPreviewController[] = [];

afterEach(() => {
    for (const controller of controllers) controller.destroy();
    controllers = [];
    document.body.replaceChildren();
});

describe("createIgnoredPreviewController", () => {
    it("always renders the zero state and disables its action", () => {
        const { controller } = setup();

        expect(controller.footer.textContent).toContain("0 ignored");
        expect(controller.footer.textContent).toContain("nothing omitted");
        expect(action(controller).disabled).toBe(true);
    });

    it("defaults to expanded and offers to hide every ignored region", () => {
        const { preview, controller } = setup(2);

        expect(preview.classList.contains("ignored-preview-collapsed")).toBe(false);
        expect(controller.footer.textContent).toContain("2 ignored");
        expect(controller.footer.textContent).toContain("shown in Preview");
        expect(action(controller).getAttribute("aria-label")).toBe(
            "Hide all ignored content in Preview",
        );
        expect(action(controller).querySelector(".codicon-eye-closed")).not.toBeNull();
    });

    it("collapses globally, persists the preference, and offers to show the regions", () => {
        const storage = memoryStorage();
        const { preview, controller } = setup(2, storage);

        action(controller).click();

        expect(preview.classList.contains("ignored-preview-collapsed")).toBe(true);
        expect(controller.footer.textContent).toContain("hidden in Preview");
        expect(action(controller).getAttribute("aria-label")).toBe(
            "Show all ignored content in Preview",
        );
        expect(action(controller).querySelector(".codicon-eye")).not.toBeNull();
        expect(storage.getItem("dd-ignored-preview-collapsed")).toBe("1");
        expect(
            preview.querySelector(".dd-preview-ignored-region")?.getAttribute("aria-label"),
        ).toBe("Ignored Table · 3 lines");
    });

    it("restores one view preference in a newly created Preview", () => {
        const storage = memoryStorage();
        storage.setItem("dd-ignored-preview-collapsed", "1");

        const { preview } = setup(1, storage);

        expect(preview.classList.contains("ignored-preview-collapsed")).toBe(true);
    });

    it("recounts a rerendered document without losing its collapsed state", () => {
        const { preview, controller } = setup(1);
        action(controller).click();
        preview.replaceChildren(region(), region(), region());

        controller.refresh();

        expect(controller.footer.textContent).toContain("3 ignored");
        expect(preview.classList.contains("ignored-preview-collapsed")).toBe(true);
    });

    it("continues to work when storage is unavailable", () => {
        const storage = throwingStorage();
        const { preview, controller } = setup(1, storage);

        action(controller).click();

        expect(preview.classList.contains("ignored-preview-collapsed")).toBe(true);
    });
});

function setup(
    count = 0,
    storage: Storage | undefined = memoryStorage(),
): { preview: HTMLElement; controller: IgnoredPreviewController } {
    const preview = document.createElement("div");
    preview.replaceChildren(...Array.from({ length: count }, region));
    const controller = createIgnoredPreviewController(preview, storage);
    controllers.push(controller);
    document.body.append(preview, controller.footer);
    return { preview, controller };
}

function region(): HTMLElement {
    const element = document.createElement("div");
    element.className = "dd-preview-ignored-region";
    element.dataset.ignoredSummary = "Table · 3 lines";
    return element;
}

function action(controller: IgnoredPreviewController): HTMLButtonElement {
    return controller.footer.querySelector(".dd-ignored-preview-toggle")!;
}

function memoryStorage(): Storage {
    const entries = new Map<string, string>();
    return {
        get length() {
            return entries.size;
        },
        clear: () => entries.clear(),
        getItem: (key) => entries.get(key) ?? null,
        key: (index) => [...entries.keys()][index] ?? null,
        removeItem: (key) => {
            entries.delete(key);
        },
        setItem: (key, value) => {
            entries.set(key, value);
        },
    };
}

function throwingStorage(): Storage {
    return {
        length: 0,
        clear: () => {
            throw new Error("denied");
        },
        getItem: () => {
            throw new Error("denied");
        },
        key: () => {
            throw new Error("denied");
        },
        removeItem: () => {
            throw new Error("denied");
        },
        setItem: () => {
            throw new Error("denied");
        },
    };
}
