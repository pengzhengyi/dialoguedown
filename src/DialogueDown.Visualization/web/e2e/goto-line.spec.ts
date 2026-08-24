import { test, expect } from "@playwright/test";
import { SAMPLE_SOURCE, SAMPLE_STAGES, writeReport } from "./report";

// SAMPLE_SOURCE is several lines long, so a jump has somewhere to land.
const url = writeReport({ source: SAMPLE_SOURCE, stages: SAMPLE_STAGES, mode: "edit" });

const dialog = ".source-pane .dd-goto";
const field = `${dialog} .dd-goto-input`;
const guidance = `${dialog} .dd-goto-guidance`;

// Several gutters carry `cm-activeLineGutter`; only the line-number one shows the line.
const activeLine = (pane: string) => `${pane} .cm-lineNumbers .cm-activeLineGutter`;

test("goes to a line on VS Code's shortcut, and leaves the cursor there", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();

    // Literal Control on every platform, exactly as VS Code binds Go to Line.
    await page.keyboard.press("Control+g");

    await expect(page.locator(field)).toBeFocused();
    await page.locator(field).fill("3");
    await page.keyboard.press("Enter");

    await expect(page.locator(activeLine(".source-pane"))).toHaveText("3");
    // The dialog closes behind the jump rather than sitting over the document.
    await expect(page.locator(dialog)).toHaveCount(0);
});

test("says what Enter will do, and keeps saying it as the field is typed into", async ({
    page,
}) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Control+g");

    // Line 7 is the Alice line, long enough for column 5 to be a real place.
    await page.locator(field).fill("7:5");
    await expect(page.locator(guidance)).toHaveText("Press Enter to go to line 7 at column 5.");

    await page.locator(field).fill("");
    // With nothing to go to, the sentence teaches the expression instead.
    await expect(page.locator(guidance)).toContainText("50%");
});

test("stays a slim two-row box rather than a form-sized one", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Control+g");

    // Pico sizes inputs for a form; unchecked, the field alone is taller than the whole box
    // should be. Long and thin is the shape: wide enough to read, no taller than two rows.
    const box = (await page.locator(dialog).boundingBox())!;
    const input = (await page.locator(field).boundingBox())!;

    expect(input.height).toBeLessThan(34);
    expect(box.height).toBeLessThan(80);
    expect(box.width).toBeGreaterThan(box.height * 4);
});

test("prompts for the column range the moment the colon is typed", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Control+g");

    await page.locator(field).fill("3:");

    // The range belongs to that line, so it has to name the line as well as the bound.
    await expect(page.locator(guidance)).toContainText("Type a column between 1 and");
    await expect(page.locator(guidance)).toContainText("on line 3");
});

test("counts columns from one, so :1 is the start of the line", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Control+g");

    await page.locator(field).fill("7:1");
    await page.keyboard.press("Enter");
    // CodeMirror counts columns from zero, which would land `:1` one character in. Typing is the
    // unambiguous check: at column 1 the character must land before everything already there.
    await page.keyboard.type("X");

    await expect(page.locator(".source-pane .cm-content")).toContainText("XAlice:");
});

test("offers the column once a line is entered", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Control+g");

    await page.locator(field).fill("9");
    await expect(page.locator(guidance)).toContainText("add :5 for a column");

    // Once a column is given the offer has served its purpose and goes away.
    await page.locator(field).fill("9:5");
    await expect(page.locator(guidance)).toHaveText("Press Enter to go to line 9 at column 5.");
});

test("offers no button — Enter goes, and there is nothing else to press", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Control+g");

    await expect(page.locator(`${dialog} button:visible`)).toHaveCount(0);
});

test("takes a relative offset, which the sentence resolves before the jump", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Control+g");
    await page.locator(field).fill("5");
    await page.keyboard.press("Enter");
    await expect(page.locator(activeLine(".source-pane"))).toHaveText("5");

    await page.keyboard.press("Control+g");
    await page.locator(field).fill("-2");
    await expect(page.locator(guidance)).toContainText("Press Enter to go to line 3");
    await page.keyboard.press("Enter");

    await expect(page.locator(activeLine(".source-pane"))).toHaveText("3");
});

test("Escape dismisses it without moving the cursor", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    // Wherever the click left the cursor is where it must still be afterwards.
    const before = await page.locator(activeLine(".source-pane")).textContent();
    await page.keyboard.press("Control+g");
    await expect(page.locator(dialog)).toBeVisible();

    await page.keyboard.press("Escape");

    await expect(page.locator(dialog)).toHaveCount(0);
    await expect(page.locator(activeLine(".source-pane"))).toHaveText(before ?? "");
});

test("clicking away dismisses it, the way a quick input does", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Control+g");
    await expect(page.locator(dialog)).toBeVisible();

    await page.locator(".source-preview").click({ position: { x: 10, y: 10 } });

    await expect(page.locator(dialog)).toHaveCount(0);
});

test("floats over the document instead of pushing it up", async ({ page }) => {
    await page.goto(url);
    const editor = page.locator(".source-pane .cm-content");
    await editor.click();
    const before = await editor.boundingBox();

    await page.keyboard.press("Control+g");
    await expect(page.locator(dialog)).toBeVisible();

    // Opening it must not reflow the text: same height, same top.
    const during = await editor.boundingBox();
    expect(during!.height).toBeCloseTo(before!.height, 0);
    expect(during!.y).toBeCloseTo(before!.y, 0);

    // And it sits near the top of the editor, not at its foot, the way VS Code's does.
    const box = await page.locator(dialog).boundingBox();
    expect(box!.y).toBeLessThan(before!.y + before!.height / 2);
});

test("its field and guidance are legible, in the theme the report is wearing", async ({ page }) => {
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Control+g");

    // Both would still be "visible" to a selector while painting text onto its own background —
    // CodeMirror's light-themed `.cm-textfield` did exactly that to the field on the dark theme.
    for (const selector of [field, guidance]) {
        const painted = await page.locator(selector).evaluate((node) => {
            const style = getComputedStyle(node);
            return { color: style.color, background: style.backgroundColor };
        });
        expect(painted.color).not.toBe(painted.background);
    }
});

test("its field follows the dark theme rather than a library default", async ({ page }) => {
    await page.emulateMedia({ colorScheme: "dark" });
    await page.goto(url);
    await page.locator(".source-pane .cm-content").click();
    await page.keyboard.press("Control+g");

    const background = await page
        .locator(field)
        .evaluate((node) => getComputedStyle(node).backgroundColor);

    // A white field on a dark report is the regression this guards.
    expect(background).not.toBe("rgb(255, 255, 255)");
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

    await page.keyboard.press("Control+g");
    await page.locator(".playbook-source .dd-goto .dd-goto-input").fill("4");
    await page.keyboard.press("Enter");

    await expect(page.locator(activeLine(".playbook-source"))).toHaveText("4");
});
