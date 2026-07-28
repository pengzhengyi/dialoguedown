import { mkdirSync, writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { spawnCli } from "./cli-runner.mjs";
import {
    SHELL_TREE,
    SHELL_TOP_DOC,
    SHELL_SUB_DOC,
    SHELL_TOP_SOURCE,
    SHELL_SUB_SOURCE,
    SHELL_PORT,
} from "./fixture.mjs";

// The Playwright webServer for the empty-shell e2e. Builds a small tree — a script
// at the root and one in a sub-folder — then runs the real .NET server (visualize
// with a root but no source, so it lands on the empty shell: the Explorer over the
// project with no document open) on the fixed loopback port. Playwright waits for
// the URL to respond, runs the specs, and terminates this process tree on teardown.
mkdirSync(dirname(SHELL_SUB_DOC), { recursive: true });
writeFileSync(SHELL_TOP_DOC, SHELL_TOP_SOURCE);
writeFileSync(SHELL_SUB_DOC, SHELL_SUB_SOURCE);

spawnCli(["visualize", "--root", SHELL_TREE, "--port", String(SHELL_PORT), "--no-open"]);
