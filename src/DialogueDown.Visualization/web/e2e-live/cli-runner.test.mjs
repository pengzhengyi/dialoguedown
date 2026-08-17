import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import test from "node:test";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { cliInvocation } from "./cli-runner.mjs";

const here = dirname(fileURLToPath(import.meta.url));

test("the CLI invocation runs the built Release DLL directly", () => {
    const invocation = cliInvocation(["visualize", "scene.dialogue.md"]);

    assert.equal(invocation.command, "dotnet");
    // The framework is read from the CLI project, so this follows it instead of pinning a copy.
    assert.match(
        invocation.args[0],
        /DialogueDown\.Cli\/bin\/Release\/net\d+\.\d+\/DialogueDown\.Cli\.dll$/,
    );
    assert.deepEqual(invocation.args.slice(1), ["visualize", "scene.dialogue.md"]);
});

test("every live E2E server uses the shared CLI runner", () => {
    const serverScripts = readdirSync(here).filter(
        (name) => name.startsWith("serve") && name.endsWith(".mjs"),
    );

    assert.equal(serverScripts.length, 9);
    for (const name of serverScripts) {
        const source = readFileSync(join(here, name), "utf8");
        assert.match(source, /spawnCli/);
        assert.doesNotMatch(source, /"run"/);
        assert.doesNotMatch(source, /"--project"/);
    }
});
