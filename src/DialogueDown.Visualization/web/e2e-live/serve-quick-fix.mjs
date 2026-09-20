import { writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { spawnCli } from "./cli-runner.mjs";
import { QUICK_FIX_DOC, QUICK_FIX_PORT, QUICK_FIX_SOURCE } from "./fixture.mjs";

// The Playwright webServer for the quick-fix e2e: write a fresh temp document with a dangling
// arrow, then run the real .NET server in --edit (editable) mode on the fixed loopback port.
writeFileSync(QUICK_FIX_DOC, QUICK_FIX_SOURCE);

spawnCli([
    "visualize",
    QUICK_FIX_DOC,
    "--edit",
    "--root",
    dirname(QUICK_FIX_DOC),
    "--port",
    String(QUICK_FIX_PORT),
    "--no-open",
]);
