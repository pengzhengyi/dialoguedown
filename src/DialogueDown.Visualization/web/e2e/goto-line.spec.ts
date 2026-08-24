import { test, expect } from "@playwright/test";
import { SAMPLE_SOURCE, SAMPLE_STAGES, writeReport } from "./report";

// SAMPLE_SOURCE is several lines long, so a jump has somewhere to land.
const url = writeReport({ source: SAMPLE_SOURCE, stages: SAMPLE_STAGES, mode: "edit" });

const dialog = ".source-pane .cm-panel form";

// Several gutters carry `cm-activeLineGutter`; only the line-number one shows the line.
const activeLine = (pane: string) => `${pane} .cm-lineNumbers .cm-activeLineGutter`;

test("goes to a line on the shortcut, and leaves the cursor there", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();

    await page.keyboard.press("ControlOrMeta+Alt+g");

    const field = page.locator(`${dialog} input[name="line"]`);
    await expect(field).toBeVisible();
    await field.fill("3");
    await page.keyboard.press("Enter");

    await expect(page.locator(activeLine(".source-pane"))).toHaveText("3");
    // The dialog closes behind the jump rather than sitting over the document.
    await expect(page.locator(dialog)).toHaveCount(0);
});

test("takes a relative offset, which is CodeMirror's own parsing and not ours", async ({
    page,
}) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("ControlOrMeta+Alt+g");
    await page.locator(`${dialog} input[name="line"]`).fill("5");
    await page.keyboard.press("Enter");
    await expect(page.locator(activeLine(".source-pane"))).toHaveText("5");

    await page.keyboard.press("ControlOrMeta+Alt+g");
    await page.locator(`${dialog} input[name="line"]`).fill("-2");
    await page.keyboard.press("Enter");

    await expect(page.locator(activeLine(".source-pane"))).toHaveText("3");
});

test("Escape dismisses it without moving the cursor", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    // Wherever the click left the cursor is where it must still be afterwards.
    const before = await page.locator(activeLine(".source-pane")).textContent();
    await page.keyboard.press("ControlOrMeta+Alt+g");
    await expect(page.locator(dialog)).toBeVisible();

    await page.keyboard.press("Escape");

    await expect(page.locator(dialog)).toHaveCount(0);
    await expect(page.locator(activeLine(".source-pane"))).toHaveText(before ?? "");
});

test("wears the report's chrome rather than the browser's form defaults", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("ControlOrMeta+Alt+g");

    // One compact row: the label, the field, and the button sit on a single line.
    const box = await page.locator(dialog).boundingBox();
    expect(box!.height).toBeLessThan(48);

    // The field is sized to a line number, not stretched across the pane.
    const field = await page.locator(`${dialog} input[name="line"]`).boundingBox();
    expect(field!.width).toBeLessThan(200);
    expect(field!.y).toBeLessThan(box!.y + box!.height);
});

test("its buttons are legible — Pico paints button text white by default", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("ControlOrMeta+Alt+g");

    // The submit button was white-on-white until `--pico-color` was reset, and "visible" to a
    // selector the whole time, so assert the two colors actually differ.
    for (const selector of [`${dialog} .cm-button`, ".source-pane .cm-panel .cm-dialog-close"]) {
        const painted = await page.locator(selector).evaluate((node) => {
            const style = getComputedStyle(node);
            return { color: style.color, background: style.backgroundColor };
        });
        expect(painted.color).not.toBe(painted.background);
        expect(painted.color).not.toBe("rgb(255, 255, 255)");
    }
});

test("is offered in the Playbook editor too, which is read-only but still navigable", async ({
    page,
}) => {
    await page.goto(
        writeReport({
            source: SAMPLE_SOURCE,
            stages: SAMPLE_STAGES,
            playbook: {
                json: '{\n  "a": 1,\n  "b": 2,\n  "c": 3,\n  "d": 4\n}',
                anchors: [],
                speakers: [],
            },
        }),
    );
    await page.click("#tabs .tab:last-child");
    await page.locator(".playbook-source .cm-content").click();

    await page.keyboard.press("ControlOrMeta+Alt+g");
    await page.locator('.playbook-source .cm-panel form input[name="line"]').fill("4");
    await page.keyboard.press("Enter");

    await expect(page.locator(activeLine(".playbook-source"))).toHaveText("4");
});
