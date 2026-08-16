import { afterEach, describe, expect, it } from "vitest";
import { createIgnoredPreviewController, type IgnoredPreviewController } from "./ignored-preview";

let controllers: IgnoredPreviewController[] = [];

afterEach(() => {
    for (const controller of controllers) controller.destroy();
    controllers = [];
    document.body.replaceChildren();
});

describe("createIgnoredPreviewController", () => {
    it("always renders the zero state and disables both commands", () => {
        const { controller } = setup([]);

        expect(controller.footer.textContent).toContain("0 ignored");
        expect(controller.footer.textContent).toContain("nothing omitted");
        expect(command(controller, "expand").disabled).toBe(true);
        expect(command(controller, "collapse").disabled).toBe(true);
    });

    it("defaults to showing every region and names each control", () => {
        const { preview, controller } = setup(["a", "b"]);

        expect(hiddenKeys(preview)).toEqual([]);
        expect(controller.footer.textContent).toContain("2 ignored");
        expect(controller.footer.textContent).toContain("all shown in Preview");
        expect(command(controller, "expand").disabled).toBe(false);
        expect(command(controller, "collapse").disabled).toBe(false);

        const control = toggle(preview, "a");
        expect(control.getAttribute("aria-expanded")).toBe("true");
        expect(control.getAttribute("aria-label")).toBe("Hide ignored Table · 3 lines");
    });

    it("hides every region on Collapse all and persists the baseline", () => {
        const storage = memoryStorage();
        const { preview, controller } = setup(["a", "b"], storage);

        command(controller, "collapse").click();

        expect(hiddenKeys(preview)).toEqual(["a", "b"]);
        expect(controller.footer.textContent).toContain("all hidden in Preview");
        expect(storage.getItem("dd-ignored-preview-collapsed")).toBe("1");
        expect(toggle(preview, "a").getAttribute("aria-expanded")).toBe("false");
        expect(toggle(preview, "a").getAttribute("aria-label")).toBe(
            "Show ignored Table · 3 lines",
        );
    });

    it("shows every region on Expand all and clears the persisted baseline", () => {
        const storage = memoryStorage();
        storage.setItem("dd-ignored-preview-collapsed", "1");
        const { preview, controller } = setup(["a"], storage);

        command(controller, "expand").click();

        expect(hiddenKeys(preview)).toEqual([]);
        expect(storage.getItem("dd-ignored-preview-collapsed")).toBeNull();
    });

    it("restores the persisted baseline in a newly created Preview", () => {
        const storage = memoryStorage();
        storage.setItem("dd-ignored-preview-collapsed", "1");

        const { preview } = setup(["a"], storage);

        expect(hiddenKeys(preview)).toEqual(["a"]);
    });

    it("lets one region differ from the baseline and reports the mixed view", () => {
        const { preview, controller } = setup(["a", "b", "c"]);

        toggle(preview, "b").click();

        expect(hiddenKeys(preview)).toEqual(["b"]);
        expect(controller.footer.textContent).toContain("2 of 3 shown in Preview");
        expect(toggle(preview, "b").getAttribute("aria-expanded")).toBe("false");
    });

    it("shows a region that differs from a hidden baseline", () => {
        const storage = memoryStorage();
        storage.setItem("dd-ignored-preview-collapsed", "1");
        const { preview, controller } = setup(["a", "b"], storage);

        toggle(preview, "a").click();

        expect(hiddenKeys(preview)).toEqual(["b"]);
        expect(controller.footer.textContent).toContain("1 of 2 shown in Preview");
    });

    it("overrides every individual choice when a global command runs", () => {
        const { preview, controller } = setup(["a", "b"]);

        toggle(preview, "a").click();
        command(controller, "collapse").click();
        expect(hiddenKeys(preview)).toEqual(["a", "b"]);

        toggle(preview, "b").click();
        command(controller, "expand").click();

        expect(hiddenKeys(preview)).toEqual([]);
        expect(controller.footer.textContent).toContain("all shown in Preview");
    });

    it("keeps a region's choice when the Preview is rendered again", () => {
        const { preview, controller } = setup(["a", "b"]);
        toggle(preview, "a").click();

        preview.replaceChildren(region("a"), region("b"), region("c"));
        controller.refresh();

        expect(hiddenKeys(preview)).toEqual(["a"]);
        expect(controller.footer.textContent).toContain("2 of 3 shown in Preview");
    });

    it("returns an edited region to the baseline", () => {
        const { preview, controller } = setup(["a", "b"]);
        toggle(preview, "a").click();

        preview.replaceChildren(region("a-edited"), region("b"));
        controller.refresh();

        expect(hiddenKeys(preview)).toEqual([]);
        expect(controller.footer.textContent).toContain("all shown in Preview");
    });

    it("continues to work when storage is unavailable", () => {
        const { preview, controller } = setup(["a"], throwingStorage());

        command(controller, "collapse").click();

        expect(hiddenKeys(preview)).toEqual(["a"]);
    });

    it("stops handling region clicks once destroyed", () => {
        const { preview, controller } = setup(["a"]);

        controller.destroy();
        toggle(preview, "a").click();

        expect(hiddenKeys(preview)).toEqual([]);
    });
});

function setup(
    keys: readonly string[],
    storage: Storage | undefined = memoryStorage(),
): { preview: HTMLElement; controller: IgnoredPreviewController } {
    const preview = document.createElement("div");
    preview.replaceChildren(...keys.map(region));
    const controller = createIgnoredPreviewController(preview, storage);
    controllers.push(controller);
    document.body.append(preview, controller.footer);
    return { preview, controller };
}

function region(key: string): HTMLElement {
    const element = document.createElement("div");
    element.className = "dd-preview-ignored-region";
    element.dataset.ignoredSummary = "Table · 3 lines";
    element.dataset.ignoredKey = key;
    const control = document.createElement("button");
    control.type = "button";
    control.className = "dd-ignored-region-toggle";
    element.append(control);
    return element;
}

function toggle(preview: HTMLElement, key: string): HTMLButtonElement {
    return preview.querySelector(`[data-ignored-key="${key}"] .dd-ignored-region-toggle`)!;
}

function hiddenKeys(preview: HTMLElement): string[] {
    return [...preview.querySelectorAll<HTMLElement>(".dd-ignored-region-hidden")].map(
        (region) => region.dataset.ignoredKey ?? "",
    );
}

function command(
    controller: IgnoredPreviewController,
    name: "expand" | "collapse",
): HTMLButtonElement {
    return controller.footer.querySelector(`[data-command="${name}"]`)!;
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
