import { writeFileSync } from "node:fs";
import { dirname } from "node:path";
import { spawnCli } from "./cli-runner.mjs";
import { FIX_ALL_DOC, FIX_ALL_PORT, FIX_ALL_SOURCE } from "./fixture.mjs";

// The Playwright webServer for the fix-all e2e: write a fresh temp document with two dangling
// arrows, then run the real .NET server in --edit (editable) mode on the fixed loopback port.
writeFileSync(FIX_ALL_DOC, FIX_ALL_SOURCE);

spawnCli([
    "visualize",
    FIX_ALL_DOC,
    "--edit",
    "--root",
    dirname(FIX_ALL_DOC),
    "--port",
    String(FIX_ALL_PORT),
    "--no-open",
]);
