import { describe, expect, it } from "vitest";
import {
    LINE_DEBUGGER_FIXTURE_ID,
    LINE_DEBUGGER_SOURCE,
    createLineDebuggerPrototype,
} from "./debug-fixture";

describe("createLineDebuggerPrototype", () => {
    it("leaves ordinary reports unchanged", () => {
        expect(createLineDebuggerPrototype(LINE_DEBUGGER_SOURCE, "")).toBeUndefined();
        expect(
            createLineDebuggerPrototype(LINE_DEBUGGER_SOURCE, "?debug=fake&fixture=unknown"),
        ).toBeUndefined();
    });

    it("creates the registered fake controller only for the explicit fixture query", () => {
        const debug = createLineDebuggerPrototype(
            LINE_DEBUGGER_SOURCE,
            `?debug=fake&fixture=${LINE_DEBUGGER_FIXTURE_ID}`,
        );

        expect(debug?.snapshot()).toMatchObject({ status: "ready" });
    });

    it("never fabricates locations when the registered fixture cannot bind", () => {
        const debug = createLineDebuggerPrototype(
            "Not the dedicated sample.",
            `?debug=fake&fixture=${LINE_DEBUGGER_FIXTURE_ID}`,
        );

        expect(debug?.snapshot()).toMatchObject({ status: "unavailable" });
    });
});
