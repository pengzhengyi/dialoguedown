import assert from "node:assert/strict";
import { statSync } from "node:fs";
import test from "node:test";

const MAX_REPORT_BYTES = 5_000_000;
const report = new URL("../dist/report.html", import.meta.url);

function assertReportSize(bytes, maximum = MAX_REPORT_BYTES) {
    assert.ok(
        bytes <= maximum,
        `dist/report.html is ${bytes.toLocaleString("en-US")} bytes; ` +
            `the approved limit is ${maximum.toLocaleString("en-US")} bytes`,
    );
}

test("the self-contained report stays under the approved 5 MB raw limit", () => {
    assertReportSize(statSync(report).size);
});

test("the bundle guard reports both the measured size and the limit", () => {
    assert.throws(
        () => assertReportSize(5_000_001),
        /5,000,001 bytes; the approved limit is 5,000,000 bytes/,
    );
});
