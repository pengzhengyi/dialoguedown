import { describe, it, expect, beforeEach, vi } from "vitest";
import {
    readThemePreference,
    applyThemePreference,
    createThemeSelect,
    createThemeControl,
    initTheme,
    themeIcon,
    type ThemePreference,
} from "./theme";

/** A minimal in-memory Storage for isolated, deterministic tests. */
function fakeStorage(seed: Record<string, string> = {}): Storage {
    const map = new Map(Object.entries(seed));
    return {
        getItem: (k) => map.get(k) ?? null,
        setItem: (k, v) => void map.set(k, String(v)),
        removeItem: (k) => void map.delete(k),
        clear: () => map.clear(),
        key: (i) => [...map.keys()][i] ?? null,
        get length() {
            return map.size;
        },
    } as Storage;
}

describe("readThemePreference", () => {
    it("defaults to system when nothing is stored", () => {
        expect(readThemePreference(fakeStorage())).toBe("system");
    });

    it("reads a stored light/dark choice", () => {
        expect(readThemePreference(fakeStorage({ "dd-theme": "dark" }))).toBe("dark");
        expect(readThemePreference(fakeStorage({ "dd-theme": "light" }))).toBe("light");
    });

    it("ignores an unknown stored value", () => {
        expect(readThemePreference(fakeStorage({ "dd-theme": "neon" }))).toBe("system");
    });

    it("falls back to system when storage throws", () => {
        const throwing = {
            getItem: () => {
                throw new Error("blocked");
            },
        } as unknown as Storage;
        expect(readThemePreference(throwing)).toBe("system");
    });
});

describe("applyThemePreference", () => {
    let root: HTMLElement;
    beforeEach(() => {
        root = document.createElement("html");
    });

    it("sets data-theme and persists for a forced theme", () => {
        const storage = fakeStorage();
        applyThemePreference("dark", root, storage);
        expect(root.getAttribute("data-theme")).toBe("dark");
        expect(storage.getItem("dd-theme")).toBe("dark");
    });

    it("clears data-theme and the stored value for system", () => {
        const storage = fakeStorage({ "dd-theme": "light" });
        root.setAttribute("data-theme", "light");
        applyThemePreference("system", root, storage);
        expect(root.hasAttribute("data-theme")).toBe(false);
        expect(storage.getItem("dd-theme")).toBeNull();
    });

    it("still applies the theme when storage throws", () => {
        const throwing = {
            setItem: () => {
                throw new Error("blocked");
            },
            removeItem: () => {
                throw new Error("blocked");
            },
        } as unknown as Storage;
        expect(() => applyThemePreference("dark", root, throwing)).not.toThrow();
        expect(root.getAttribute("data-theme")).toBe("dark");
    });
});

describe("createThemeSelect", () => {
    it("offers System, Light, and Dark and reflects the initial choice", () => {
        const select = createThemeSelect(vi.fn(), "dark");
        expect([...select.options].map((o) => o.value)).toEqual(["system", "light", "dark"]);
        expect(select.value).toBe("dark");
        expect(select.getAttribute("aria-label")).toBe("Color theme");
    });

    it("applies the chosen preference on change", () => {
        const apply = vi.fn();
        const select = createThemeSelect(apply, "system");
        select.value = "light";
        select.dispatchEvent(new Event("change"));
        expect(apply).toHaveBeenCalledWith<[ThemePreference]>("light");
    });
});

describe("themeIcon", () => {
    it("returns a distinct SVG icon for each preference", () => {
        const icons = [themeIcon("system"), themeIcon("light"), themeIcon("dark")];
        expect(new Set(icons).size).toBe(3);
        for (const svg of icons) expect(svg).toContain("<svg");
    });
});

describe("createThemeControl", () => {
    it("pairs the select with the icon for the initial preference", () => {
        const control = createThemeControl(vi.fn(), "dark");
        expect(control.querySelector(".theme-select")).not.toBeNull();
        // the moon path distinguishes the dark icon
        expect(control.querySelector(".theme-icon")!.innerHTML).toContain("12.79");
    });

    it("updates the icon and applies the choice on change", () => {
        const apply = vi.fn();
        const control = createThemeControl(apply, "system");
        const select = control.querySelector<HTMLSelectElement>(".theme-select")!;
        select.value = "light";
        select.dispatchEvent(new Event("change"));
        expect(apply).toHaveBeenCalledWith<[ThemePreference]>("light");
        // the sun icon uses a circle; the monitor (system) icon uses a rect
        expect(control.querySelector(".theme-icon")!.innerHTML).toContain("<circle");
        expect(control.querySelector(".theme-icon")!.innerHTML).not.toContain("<rect");
    });
});

describe("initTheme", () => {
    it("reports an effective System-theme change and removes its listener on dispose", () => {
        vi.stubGlobal("localStorage", fakeStorage());
        let listener: (() => void) | undefined;
        const remove = vi.fn();
        vi.stubGlobal(
            "matchMedia",
            vi.fn(() => ({
                matches: false,
                addEventListener: (_event: string, next: () => void) => (listener = next),
                removeEventListener: remove,
            })),
        );
        const changed = vi.fn();

        const dispose = initTheme(document.createElement("div"), changed);
        listener?.();
        dispose();

        expect(changed).toHaveBeenCalledOnce();
        expect(remove).toHaveBeenCalledWith("change", listener);
        vi.unstubAllGlobals();
    });
});
