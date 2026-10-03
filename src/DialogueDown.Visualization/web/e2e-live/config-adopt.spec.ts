import { test, expect } from "@playwright/test";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import {
    CONFIG_ADOPT_PORT,
    CONFIG_ADOPT_TREE,
    CONFIG_ADOPT_DOC,
    CONFIG_ADOPT_TOML,
    CONFIG_ADOPT_SOURCE,
    CONFIG_ADOPT_CONFIG,
} from "./fixture.mjs";

// Config Create recovery end-to-end against the real .NET --edit server: the session starts
// config-less, but a *different* dialogue.toml already exists on disk when the writer clicks
// "Create dialogue.toml". The server adopts that existing file without overwriting it instead of
// dead-ending, and the reload opens the existing Config.
const base = `http://127.0.0.1:${CONFIG_ADOPT_PORT}`;

test.beforeEach(() => {
    mkdirSync(CONFIG_ADOPT_TREE, { recursive: true });
    writeFileSync(CONFIG_ADOPT_DOC, CONFIG_ADOPT_SOURCE);
    // A hand-written config already on disk — the session was launched config-less, so it does not
    // yet apply it until the create-config request adopts it.
    writeFileSync(CONFIG_ADOPT_TOML, CONFIG_ADOPT_CONFIG);
});

test("adopts an existing dialogue.toml from the no-config state instead of failing", async ({
    page,
}) => {
    await page.goto(base);
    await page.locator(".tab", { hasText: "Config" }).click();

    // The session is config-less, so the no-config create call to action is shown.
    await expect(page.locator(".config-empty-state")).toBeVisible();
    await page.locator(".config-create-button").click();

    // The page reloads onto the editable Config tab showing the *existing* file's content — the
    // adopted config, not the starter template — and the existing file is left untouched on disk.
    await expect(page.locator(".tab.active", { hasText: "Config" })).toBeVisible();
    await expect(page.locator(".config-source .cm-content")).toContainText("Zelda");
    expect(existsSync(CONFIG_ADOPT_TOML)).toBe(true);
    expect(readFileSync(CONFIG_ADOPT_TOML, "utf8")).toBe(CONFIG_ADOPT_CONFIG);
});
