import { defineConfig, devices } from "@playwright/test";

// e2e tests load the actual built report (dist/report.html) via page.setContent
// after injecting sample stage data into the __STAGES__ slot, so no web server is
// needed — the report is fully self-contained.
export default defineConfig({
    testDir: "./e2e",
    fullyParallel: true,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 1 : 0,
    // A CI failure is only as diagnosable as what the run leaves behind. `list` alone prints an
    // assertion and discards everything else, so a flake that will not reproduce locally costs a
    // re-run to see at all. The trace carries a DOM snapshot, console, and network per action, and
    // in CI the HTML report packages it for the upload step to keep.
    //
    // `on-first-retry` rather than `retain-on-failure`: the latter records every test and throws
    // the tracing away on success, measured here at +48% on this suite — enough to slow the suite
    // and to destabilize the timing-sensitive tests. CI retries once, so a test that genuinely
    // fails is traced on its retry, and a green run pays nothing. Debugging locally, where there
    // is no retry, pass `--trace on`.
    reporter: process.env.CI ? [["list"], ["html", { open: "never" }]] : "list",
    use: {
        trace: "on-first-retry",
        screenshot: "only-on-failure",
    },
    projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
