import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";

const report = readFileSync(new URL("../dist/report.html", import.meta.url), "utf8");
const main = readFileSync(new URL("../src/main.ts", import.meta.url), "utf8");

test("the production report contains no fake debugger activation path", () => {
    assert.doesNotMatch(main, /createLineDebuggerPrototype|debug=fake|test-support/);
    assert.doesNotMatch(
        report,
        /line-debugger-v1|Prototype · fake program|Line debugger prototype|debug=fake/,
    );
});
