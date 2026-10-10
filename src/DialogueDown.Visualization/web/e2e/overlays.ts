import type { Page } from "@playwright/test";

/**
 * Let clicks and hovers pass through the panels that float over a graph.
 *
 * The legend, the zoom controls, and the detail panel sit above the canvas and can cover a node,
 * so a click or hover aimed at that node would reach the panel instead.
 */
export async function letPointerThroughPanels(page: Page): Promise<void> {
    await page.addStyleTag({
        content: ".legend, .zoom-controls, .detail { pointer-events: none !important; }",
    });
}
