import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const tasks = JSON.parse(
    readFileSync(resolve(here, "../../../../.vscode/tasks.json"), "utf8"),
).tasks;

function task(label) {
    const value = tasks.find((candidate) => candidate.label === label);
    assert.ok(value, `missing ${label} task`);
    return value;
}

test("frontend inner-loop tasks target one unit or browser scope", () => {
    assert.match(task("web: test file").command, /\$\{input:webVitestTarget\}/);
    assert.match(task("web: test watch").command, /npm run test:watch/);
    assert.match(task("web: e2e file").command, /\$\{input:webStaticE2ETarget\}/);
    assert.match(task("web: e2e grep").command, /--grep/);
    assert.match(task("web: e2e live file").command, /\$\{input:webLiveE2ETarget\}/);
});

test("frontend full verification tasks remain available", () => {
    assert.equal(task("web: check").command, "npm run check");
    assert.equal(task("web: e2e").command, "npm run e2e");
    assert.deepEqual(task("verify: all").dependsOn, [
        "format: verify",
        "build: verify",
        "test",
        "web: check",
        "web: e2e",
    ]);
});

test("full verification runs the gates a plain build cannot", () => {
    // Code-style rules do not run during dotnet build, and analyzer warnings are emitted only
    // when a project recompiles, so full verification has to format-check and rebuild fresh.
    assert.match(task("format: verify").command, /dotnet format .* --verify-no-changes/);
    assert.match(task("build: verify").command, /--no-incremental/);
});
