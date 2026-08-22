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
    reporter: process.env.CI ? [["list"], ["html", { open: "never" }]] : "list",
    use: {
        trace: "retain-on-failure",
        screenshot: "only-on-failure",
    },
    projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
