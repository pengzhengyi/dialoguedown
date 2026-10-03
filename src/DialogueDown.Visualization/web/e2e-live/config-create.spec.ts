import { test, expect } from "@playwright/test";
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import {
    CONFIG_CREATE_PORT,
    CONFIG_CREATE_TREE,
    CONFIG_CREATE_DOC,
    CONFIG_CREATE_TOML,
    CONFIG_CREATE_SOURCE,
} from "./fixture.mjs";

// Config Create New end-to-end against the real .NET --edit server: a project with no
// dialogue.toml shows the no-config state with a "Create dialogue.toml" call to action;
// clicking it writes a starter file, recompiles, and reloads onto the editable Config tab.
const base = `http://127.0.0.1:${CONFIG_CREATE_PORT}`;

test.beforeEach(() => {
    mkdirSync(CONFIG_CREATE_TREE, { recursive: true });
    rmSync(CONFIG_CREATE_TOML, { force: true }); // start from the no-config state
    writeFileSync(CONFIG_CREATE_DOC, CONFIG_CREATE_SOURCE);
});

test("creates a dialogue.toml from the no-config state and lands on the editable Config tab", async ({
    page,
}) => {
    await page.goto(base);
    await page.locator(".tab", { hasText: "Config" }).click();

    // The no-config state offers the create call to action in Edit mode.
    await expect(page.locator(".config-empty-state")).toBeVisible();
    const create = page.locator(".config-create-button");
    await expect(create).toBeVisible();

    await create.click();

    // The page reloads onto the Config tab, now the editable TOML editor seeded with the
    // starter template (the no-config state had no editor).
    await expect(page.locator(".tab.active", { hasText: "Config" })).toBeVisible();
    await expect(page.locator(".config-source .cm-editor")).toBeVisible();
    await expect(page.locator(".config-source .cm-content")).toContainText("[[speakers]]");

    expect(existsSync(CONFIG_CREATE_TOML)).toBe(true);
    expect(readFileSync(CONFIG_CREATE_TOML, "utf8")).toContain("[[speakers]]");
});
