import assert from "node:assert/strict";
import { statSync } from "node:fs";
import test from "node:test";

// What a reader downloads is the page plus the client and its stylesheet; Mermaid rides along
// only for a script that draws a diagram, so it is measured on its own. Both budgets are raw
// bytes, and both are approved limits rather than targets.
const MAX_CLIENT_BYTES = 2_000_000;
const MAX_MERMAID_BYTES = 5_000_000;
const shipped = ["report.html", "report.js", "report.css"];

function sizeOf(name) {
    return statSync(new URL(`../dist/${name}`, import.meta.url)).size;
}

function assertSize(what, bytes, maximum) {
    assert.ok(
        bytes <= maximum,
        `${what} is ${bytes.toLocaleString("en-US")} bytes; ` +
            `the approved limit is ${maximum.toLocaleString("en-US")} bytes`,
    );
}

test("the client every reader loads stays under its approved limit", () => {
    assertSize(
        "the report client",
        shipped.reduce((total, name) => total + sizeOf(name), 0),
        MAX_CLIENT_BYTES,
    );
});

test("Mermaid stays under its approved limit, and out of the client", () => {
    assertSize("dist/mermaid.js", sizeOf("mermaid.js"), MAX_MERMAID_BYTES);
    // Keeping it a separate file is the point: bundled, it would land in every page.
    assert.ok(
        sizeOf("mermaid.js") > sizeOf("report.js"),
        "Mermaid is the larger half; keep it apart",
    );
});

test("the bundle guard reports both the measured size and the limit", () => {
    assert.throws(
        () => assertSize("dist/report.js", 5_000_001, 5_000_000),
        /5,000,001 bytes; the approved limit is 5,000,000 bytes/,
    );
});
