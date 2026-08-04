import { test, expect } from "@playwright/test";
import { createServer, type ViteDevServer } from "vite";

let server: ViteDevServer;
let base = "";

test.beforeAll(async () => {
    server = await createServer({
        root: new URL("..", import.meta.url).pathname,
        logLevel: "silent",
        server: { host: "127.0.0.1", port: 0 },
    });
    await server.listen();
    base = server.resolvedUrls?.local[0] ?? "";
});

test.afterAll(async () => {
    await server.close();
});

test("renders the dormant debugger UI through a test-only browser harness", async ({ page }) => {
    await page.goto(`${base}e2e/debug-harness.html`);

    const toolbar = page.getByRole("toolbar", { name: "Line debugger" });
    await expect(toolbar).toBeVisible();
    await expect(page.locator(".dd-debug-breakpoint-gutter")).toBeVisible();
    await expect(page.locator(".dd-debug-execution-gutter")).toBeVisible();

    await toolbar.getByRole("button", { name: "Toggle breakpoint at cursor" }).click();
    const breakpoint = page.locator(".dd-debug-breakpoint-verified");
    await expect(breakpoint).toBeVisible();
    const breakpointBox = await breakpoint.boundingBox();
    expect(breakpointBox?.width).toBeGreaterThanOrEqual(8);
    expect(breakpointBox?.height).toBeGreaterThanOrEqual(8);

    await toolbar.getByRole("button", { name: "Start debugging" }).click();
    const arrow = page.locator(".dd-debug-current-arrow");
    await expect(arrow).toBeVisible();
    const arrowBox = await arrow.boundingBox();
    expect(arrowBox?.width).toBeGreaterThanOrEqual(8);
    expect(arrowBox?.height).toBeGreaterThanOrEqual(10);

    const before = await toolbar.boundingBox();
    const handle = toolbar.getByRole("button", { name: "Move debugger panel" });
    const handleBox = await handle.boundingBox();
    if (!before || !handleBox) throw new Error("Could not locate debugger palette.");
    await page.mouse.move(handleBox.x + handleBox.width / 2, handleBox.y + handleBox.height / 2);
    await page.mouse.down();
    await page.mouse.move(handleBox.x - 100, handleBox.y + 80);
    await page.mouse.up();
    const moved = await toolbar.boundingBox();
    expect(moved?.x).toBeLessThan(before.x - 30);
    expect(moved?.y).toBeGreaterThan(before.y + 30);

    await page.setViewportSize({ width: 560, height: 500 });
    await expect
        .poll(async () => {
            const [panel, pane] = await Promise.all([
                toolbar.boundingBox(),
                page.locator(".source-pane").boundingBox(),
            ]);
            return (
                !!panel &&
                !!pane &&
                panel.x >= pane.x &&
                panel.x + panel.width <= pane.x + pane.width
            );
        })
        .toBe(true);
});
