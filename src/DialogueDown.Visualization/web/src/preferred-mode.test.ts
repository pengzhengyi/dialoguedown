import { describe, it, expect } from "vitest";
import { readPreferredMode, writePreferredMode } from "./preferred-mode";

/** A stand-in for `localStorage` that fails on demand, as a sandboxed report's does. */
function storageOf(entries: Record<string, string> = {}): Storage {
    const map = new Map(Object.entries(entries));
    return {
        get length() {
            return map.size;
        },
        clear: () => map.clear(),
        getItem: (key: string) => map.get(key) ?? null,
        key: (index: number) => [...map.keys()][index] ?? null,
        removeItem: (key: string) => void map.delete(key),
        setItem: (key: string, value: string) => void map.set(key, value),
    } as Storage;
}

describe("preferred mode", () => {
    it("remembers the mode a reader chose", () => {
        const storage = storageOf();

        writePreferredMode("edit", storage);

        expect(readPreferredMode(storage)).toBe("edit");
    });

    it("has no preference until one is chosen", () => {
        expect(readPreferredMode(storageOf())).toBeUndefined();
    });

    // Storage can hold anything, including a value an older report wrote.
    it("ignores a stored value that is not a mode", () => {
        expect(readPreferredMode(storageOf({ "dd-served-mode": "static" }))).toBeUndefined();
    });

    // A sandboxed report has nowhere to remember the choice, and reading must not throw.
    it("tolerates storage that refuses to read or write", () => {
        const hostile = {
            getItem: () => {
                throw new Error("denied");
            },
            setItem: () => {
                throw new Error("denied");
            },
        } as unknown as Storage;

        expect(readPreferredMode(hostile)).toBeUndefined();
        expect(() => writePreferredMode("view", hostile)).not.toThrow();
    });

    it("tolerates storage being absent altogether", () => {
        expect(readPreferredMode(undefined)).toBeUndefined();
        expect(() => writePreferredMode("edit", undefined)).not.toThrow();
    });
});
