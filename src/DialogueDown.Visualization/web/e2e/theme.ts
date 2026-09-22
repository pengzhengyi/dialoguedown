import type { Page } from "@playwright/test";

/**
 * Wait for the document's color transitions to finish, so an appearance check reads a settled
 * color.
 *
 * Colors are animated, so the moment a state changes, every themed element is partway between its
 * old and new color. Any check that samples appearance right then — an axe contrast pass above all
 * — reads a color that belongs to neither state, and fails or passes on timing rather than on the
 * styling under test. That is exactly how a contrast check on ignored Preview links failed in CI
 * at a ratio (4.35) matching no settled color, while passing locally.
 *
 * Waiting on the transitions themselves, rather than on a fixed delay, keeps the wait both exact
 * and as short as the animation actually is. Only transitions are awaited: an indefinite CSS
 * animation would never resolve, and a `finished` promise rejects when its transition is
 * interrupted, which is a settled state for our purposes rather than a failure.
 */
export async function settleTransitions(page: Page): Promise<void> {
    await page.evaluate(async () => {
        const transitions = document
            .getAnimations()
            .filter((animation) => animation instanceof CSSTransition);
        await Promise.all(transitions.map((t) => t.finished.catch(() => undefined)));
    });
}

/** Switch the report's theme and wait for the color transitions it starts to finish. */
export async function selectTheme(page: Page, theme: "light" | "dark" | "system"): Promise<void> {
    await page.locator(".theme-select").selectOption(theme);
    await settleTransitions(page);
}

/** How many of the document's color transitions are still running. */
export async function runningTransitions(page: Page): Promise<number> {
    return page.evaluate(
        () =>
            document.getAnimations().filter((animation) => animation instanceof CSSTransition)
                .length,
    );
}
