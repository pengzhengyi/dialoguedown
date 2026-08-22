import { defineConfig, devices } from "@playwright/test";
import {
    LIVE_PORT,
    RENDER_ROOT_PORT,
    SHELL_PORT,
    LIVE_EDIT_PORT,
    CONFIG_EDIT_PORT,
    CONFIG_CREATE_PORT,
    CONFIG_ADOPT_PORT,
    CONFIG_ADOPT_INVALID_PORT,
    SEMANTIC_AUTOCOMPLETE_PORT,
    SWITCH_PORT,
} from "./e2e-live/fixture.mjs";

// Live e2e: exercises the real .NET live server end-to-end in a browser — hot
// reload, the missing-document banner, and consent-gated asset hosting. Kept
// separate from the fast static `file://` suite (playwright.config.ts) because it
// starts servers.
const baseURL = `http://127.0.0.1:${LIVE_PORT}`;

export default defineConfig({
    testDir: "./e2e-live",
    testMatch: "**/*.spec.ts",
    fullyParallel: false,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 1 : 0,
    reporter: "list",
    use: { baseURL },
    projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
    webServer: [
        {
            command: "node ./e2e-live/serve.mjs",
            url: baseURL,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
        {
            command: "node ./e2e-live/serve-renderroot.mjs",
            url: `http://127.0.0.1:${RENDER_ROOT_PORT}`,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
        {
            command: "node ./e2e-live/serve-shell.mjs",
            url: `http://127.0.0.1:${SHELL_PORT}`,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
        {
            command: "node ./e2e-live/serve-live.mjs",
            url: `http://127.0.0.1:${LIVE_EDIT_PORT}`,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
        {
            command: "node ./e2e-live/serve-config-edit.mjs",
            url: `http://127.0.0.1:${CONFIG_EDIT_PORT}`,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
        {
            command: "node ./e2e-live/serve-config-create.mjs",
            url: `http://127.0.0.1:${CONFIG_CREATE_PORT}`,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
        {
            command: "node ./e2e-live/serve-config-adopt.mjs",
            url: `http://127.0.0.1:${CONFIG_ADOPT_PORT}`,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
        {
            command: "node ./e2e-live/serve-config-adopt-invalid.mjs",
            url: `http://127.0.0.1:${CONFIG_ADOPT_INVALID_PORT}`,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
        {
            command: "node ./e2e-live/serve-semantic-autocomplete.mjs",
            url: `http://127.0.0.1:${SEMANTIC_AUTOCOMPLETE_PORT}`,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
        {
            command: "node ./e2e-live/serve-switch.mjs",
            url: `http://127.0.0.1:${SWITCH_PORT}`,
            reuseExistingServer: !process.env.CI,
            timeout: 180_000,
        },
    ],
});
