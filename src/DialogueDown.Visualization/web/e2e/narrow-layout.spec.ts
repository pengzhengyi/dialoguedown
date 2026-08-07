import { test, expect, type Page } from "@playwright/test";
import { SAMPLE_REPORT, SAMPLE_STAGES, writeReport } from "./report";

// A phone-sized viewport. Every assertion here is about geometry the browser only
// produces after layout, so these run against the real built report rather than jsdom.
const PHONE = { width: 390, height: 780 };

// The wrapping only shows up with a realistic stage count, so pad the sample out to the
// six tabs a served report carries (Source + Config + three ASTs + Semantic Model).
const MANY_TABS = writeReport({
    ...SAMPLE_REPORT,
    stages: [
        ...SAMPLE_STAGES,
        { ...SAMPLE_STAGES[0], title: "Dialogue AST" },
        { ...SAMPLE_STAGES[0], title: "Desugared AST" },
        { ...SAMPLE_STAGES[0], title: "Semantic Model" },
        { ...SAMPLE_STAGES[0], title: "Runtime Program" },
    ],
});

test.use({ viewport: PHONE });

/** The distinct row offsets the tabs occupy — one entry means they share a single line. */
async function tabRowCount(page: Page): Promise<number> {
    return page.evaluate(() => {
        const tops = [...document.querySelectorAll("button.tab")].map((t) =>
            Math.round(t.getBoundingClientRect().y),
        );
        return new Set(tops).size;
    });
}

test("keeps every stage tab on a single scrollable row", async ({ page }) => {
    await page.goto(MANY_TABS);
    await expect(page.locator(".tab").first()).toBeVisible();

    // Before the scroll strip this was 3 rows at 390px, which ate 124px of header.
    expect(await tabRowCount(page)).toBe(1);

    // The row absorbs the overflow itself instead of pushing the document sideways.
    const overflow = await page.evaluate(() => {
        const nav = document.querySelector("nav.tabs")!;
        return {
            scrollable: nav.scrollWidth > nav.clientWidth,
            docOverflows:
                document.documentElement.scrollWidth > document.documentElement.clientWidth,
        };
    });
    expect(overflow.scrollable).toBe(true);
    expect(overflow.docOverflows).toBe(false);
});

test("scrolls a restored tab back into view without a pointer", async ({ page }) => {
    await page.goto(MANY_TABS);

    // Activating by click proves nothing here: Playwright scrolls a target into view as part
    // of actionability, so the strip is already positioned before the click lands. Reload
    // instead — the tab is restored from sessionStorage onto a strip that starts at
    // scrollLeft 0, which is the case revealActiveTab actually exists for.
    await page.locator("button.tab").last().click();
    await page.reload();
    await expect(page.locator("button.tab").last()).toHaveClass(/active/);

    // Poll: the reveal is a smooth scroll, so it settles over a frame or two.
    await expect
        .poll(async () =>
            page.evaluate(() => {
                const nav = document.querySelector("nav.tabs")!.getBoundingClientRect();
                const tab = [...document.querySelectorAll("button.tab")]
                    .at(-1)!
                    .getBoundingClientRect();
                return tab.left >= nav.left - 1 && tab.right <= nav.right + 1;
            }),
        )
        .toBe(true);
});

test("keeps the focus-mode controls pinned beside the scrolling tabs", async ({ page }) => {
    await page.goto(MANY_TABS);

    // They used to be absolutely positioned against the header, so a wrapped nav stranded
    // them on the last row. They now share the tab row and stay right of the tabs.
    const placement = await page.evaluate(() => {
        const nav = document.querySelector("nav.tabs")!.getBoundingClientRect();
        const zen = document.querySelector(".tabbar-zen")!.getBoundingClientRect();
        const max = document.querySelector(".tabbar-maximize")!.getBoundingClientRect();
        return {
            zenRightOfTabs: zen.left >= nav.right - 1,
            maxRightOfZen: max.left >= zen.right - 1,
            sameRow: Math.abs(zen.y - max.y) < 4,
            insideViewport: max.right <= window.innerWidth + 1,
        };
    });
    expect(placement).toEqual({
        zenRightOfTabs: true,
        maxRightOfZen: true,
        sameRow: true,
        insideViewport: true,
    });
});

test("leaves room for a focused tab's ring inside the scroller", async ({ page }) => {
    await page.goto(MANY_TABS);

    // The row clips its cross axis so a horizontal scroller cannot sprout a vertical bar,
    // and clipping happens at the padding box. The framework paints the focus ring as a
    // spread shadow *outside* the tab's box, so without this room the ring is cut away
    // entirely and a keyboard reader cannot see where they are.
    const room = await page.evaluate(() => {
        const tab = document.querySelector("button.tab")!.getBoundingClientRect();
        const nav = document.querySelector("nav.tabs")!.getBoundingClientRect();
        return {
            above: tab.top - nav.top,
            below: nav.bottom - tab.bottom,
            clipped: getComputedStyle(document.querySelector("nav.tabs")!).overflowY,
        };
    });

    expect(room.clipped).toBe("hidden");
    expect(room.above).toBeGreaterThanOrEqual(2);
    expect(room.below).toBeGreaterThanOrEqual(2);
});

test("bounds the expanded help so it cannot starve the stage", async ({ page }) => {
    await page.goto(MANY_TABS);
    const appBefore = await page.locator("#app").boundingBox();

    await page.locator("#help-toggle").click();
    await expect(page.locator(".shortcuts")).toBeVisible();

    // Unbounded, the help panel measured 1093px in a 780px window and squeezed #app to 27px.
    const after = await page.evaluate(() => {
        const app = document.querySelector("#app")!.getBoundingClientRect();
        const shortcuts = document.querySelector(".shortcuts")!;
        return {
            appHeight: app.height,
            helpHeight: shortcuts.getBoundingClientRect().height,
            helpScrolls: shortcuts.scrollHeight > shortcuts.clientHeight,
        };
    });

    expect(after.helpHeight).toBeLessThanOrEqual(halfOf(PHONE.height));
    expect(after.helpScrolls).toBe(true);
    // The stage keeps a workable share of the window rather than collapsing to a sliver.
    expect(after.appHeight).toBeGreaterThan(200);
    expect(appBefore!.height).toBeGreaterThan(200);
});

/** The half-viewport cap the help panel is held to, with a pixel of rounding slack. */
function halfOf(height: number): number {
    return height / 2 + 1;
}

test.describe("on a short landscape window", () => {
    test.use({ viewport: { width: 667, height: 375 } });

    test("keeps the shell contained when the help panel is open", async ({ page }) => {
        await page.goto(MANY_TABS);
        await page.locator("#help-toggle").click();
        await expect(page.locator(".shortcuts")).toBeVisible();

        const shell = await page.evaluate(() => {
            window.scrollTo(0, 500);
            const scrolled = window.scrollY;
            window.scrollTo(0, 0);
            const app = document.querySelector("#app")!.getBoundingClientRect();
            const stages = document.querySelector("#stages")!.getBoundingClientRect();
            const footer = document.querySelector(".app-footer")!.getBoundingClientRect();
            const status = document.querySelector(".status-line")!.getBoundingClientRect();
            return {
                scrolled,
                stagesEscapesApp: stages.bottom > app.bottom + 1,
                footerBottom: Math.round(footer.bottom),
                windowHeight: window.innerHeight,
                statusVisible: status.height > 0 && status.bottom <= window.innerHeight + 1,
            };
        });

        // The stage floor used to be set on #stages, which made the child overflow its parent
        // and paint over the footer; it belongs on #app, which holds the box open instead.
        expect(shell.stagesEscapesApp).toBe(false);
        // The transient help panel yields, so the footer still ends at the window's edge...
        expect(shell.footerBottom).toBe(shell.windowHeight);
        // ...and the permanent status line is never the part that gets squeezed away.
        expect(shell.statusVisible).toBe(true);
        // A fixed-height shell must not leak a page scrollbar over blank space.
        expect(shell.scrolled).toBe(0);
    });
});

test("offers arrows to reach tabs a wheel-only mouse cannot scroll to", async ({ page }) => {
    await page.goto(MANY_TABS);
    const back = page.locator(".tab-arrow", { hasNotText: /x/ }).first();
    const forward = page.locator(".tab-arrow").last();

    // The row overflows here, so both arrows show; nothing is hidden to the left yet.
    await expect(back).toBeVisible();
    await expect(forward).toBeVisible();
    await expect(back).toBeDisabled();
    await expect(forward).toBeEnabled();

    const start = await page.evaluate(() => document.querySelector("nav.tabs")!.scrollLeft);
    await forward.click();
    await expect
        .poll(async () => page.evaluate(() => document.querySelector("nav.tabs")!.scrollLeft))
        .toBeGreaterThan(start);

    // Having moved off the start, going back is available again.
    await expect(back).toBeEnabled();
    await back.click();
    await expect
        .poll(async () => page.evaluate(() => document.querySelector("nav.tabs")!.scrollLeft))
        .toBe(start);
});

test("hides the arrows when the whole row already fits", async ({ page }) => {
    await page.setViewportSize({ width: 1400, height: 780 });
    await page.goto(MANY_TABS);
    await expect(page.locator(".tab").first()).toBeVisible();

    // Arrows would be noise on a wide window where no tab is off-screen.
    for (const arrow of await page.locator(".tab-arrow").all()) {
        await expect(arrow).toBeHidden();
    }
});

test.describe("with too little height for a docked help panel", () => {
    test.use({ viewport: { width: 900, height: 420 } });

    test("floats the help over the stage instead of squeezing it into a strip", async ({
        page,
    }) => {
        await page.goto(MANY_TABS);
        const appBefore = await page.locator("#app").boundingBox();

        await page.locator("#help-toggle").click();
        const panel = page.locator("#help-panel");
        await expect(panel).toBeVisible();

        const floating = await page.evaluate(() => {
            const p = document.querySelector("#help-panel")!;
            const footer = document.querySelector(".app-footer")!.getBoundingClientRect();
            const box = p.getBoundingClientRect();
            return {
                position: getComputedStyle(p).position,
                sitsAboveTheStatusLine: Math.abs(box.bottom - footer.top) < 2,
                height: box.height,
                // The tab row stays reachable: the reader is cross-referencing the help
                // against the report, so the way to another stage should not be covered.
                clearsTheTabRow:
                    box.top >= document.querySelector("nav.tabs")!.getBoundingClientRect().bottom,
            };
        });

        // Docked, the panel had to share the column and showed about one line at a time.
        expect(floating.position).toBe("absolute");
        expect(floating.sitsAboveTheStatusLine).toBe(true);
        expect(floating.height).toBeGreaterThan(120);
        expect(floating.clearsTheTabRow).toBe(true);

        // Floating means it costs the stage nothing.
        const appAfter = await page.locator("#app").boundingBox();
        expect(appAfter!.height).toBe(appBefore!.height);

        // And it closes from its own button, returning focus to the toggle that reopens it.
        await page.locator("#help-close").click();
        await expect(panel).toBeHidden();
        await expect(page.locator("#help-toggle")).toBeFocused();
    });
});
