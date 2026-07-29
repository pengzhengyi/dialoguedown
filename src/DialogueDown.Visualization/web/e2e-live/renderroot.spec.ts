import { test, expect } from "@playwright/test";
import { RENDER_ROOT_PORT } from "./fixture.mjs";

// This suite targets the second server (started with a script and --root). Its document
// lives in a sub-folder and links an image outside that folder, so the server hosts the
// common ancestor and serves the report at the sub-path under the /r mount.
const reportUrl = `http://127.0.0.1:${RENDER_ROOT_PORT}/`;

test("serves the report at the document sub-path and renders an outside image", async ({
    page,
}) => {
    await page.goto(reportUrl);

    // `/` redirects to the document's report at its sub-path under the /r mount.
    await expect(page).toHaveURL(/\/r\/proj\/$/);

    const image = page.locator(".source-preview img");
    await expect(image).toBeVisible();
    // The `../shared/out.png` link resolved and the image actually loaded.
    await expect
        .poll(async () => image.evaluate((img: HTMLImageElement) => img.naturalWidth))
        .toBeGreaterThan(0);
});
