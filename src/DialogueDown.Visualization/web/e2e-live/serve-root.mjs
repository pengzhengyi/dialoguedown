import { writeFileSync, mkdirSync } from "node:fs";
import { dirname } from "node:path";
import { spawnCli } from "./cli-runner.mjs";
import {
    SERVE_ROOT_TREE,
    SERVE_ROOT_DOC,
    SERVE_ROOT_IMAGE,
    SERVE_ROOT_SOURCE,
    SERVE_ROOT_PORT,
} from "./fixture.mjs";

// A second Playwright webServer that exercises `--root`. It lays out a tree
// where the document references an image outside its own folder, then serves the
// common ancestor explicitly (no consent prompt) so the report — served at the
// document's sub-path — can resolve the `../` image link.
const PNG_1x1 = Buffer.from(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
    "base64",
);

mkdirSync(dirname(SERVE_ROOT_DOC), { recursive: true });
mkdirSync(dirname(SERVE_ROOT_IMAGE), { recursive: true });
writeFileSync(SERVE_ROOT_IMAGE, PNG_1x1);
writeFileSync(SERVE_ROOT_DOC, SERVE_ROOT_SOURCE);

spawnCli([
    "visualize",
    SERVE_ROOT_DOC,
    "--port",
    String(SERVE_ROOT_PORT),
    "--root",
    SERVE_ROOT_TREE,
    "--no-open",
]);
