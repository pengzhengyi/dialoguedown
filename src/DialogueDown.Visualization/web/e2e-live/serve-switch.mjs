import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { spawnCli } from "./cli-runner.mjs";
import {
    SWITCH_TREE,
    SWITCH_FIRST_DOC,
    SWITCH_SECOND_DOC,
    SWITCH_FIRST_SOURCE,
    SWITCH_SECOND_SOURCE,
    SWITCH_PORT,
} from "./fixture.mjs";

// The Playwright webServer for the in-place script-switch e2e. Builds a two-script tree, then
// serves the first script in --edit so the session has a document open, an Explorer over the
// project, and the editing state a switch has to re-point. Playwright waits for the URL to
// respond, runs the spec, and terminates this process tree on teardown.
mkdirSync(dirname(SWITCH_SECOND_DOC), { recursive: true });
writeFileSync(SWITCH_FIRST_DOC, SWITCH_FIRST_SOURCE);
writeFileSync(SWITCH_SECOND_DOC, SWITCH_SECOND_SOURCE);

spawnCli([
    "visualize",
    SWITCH_FIRST_DOC,
    "--root",
    SWITCH_TREE,
    "--edit",
    "--port",
    String(SWITCH_PORT),
    "--no-open",
]);
