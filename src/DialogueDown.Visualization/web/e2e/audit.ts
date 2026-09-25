import AxeBuilder from "@axe-core/playwright";
import type { Page } from "@playwright/test";
import { settleTransitions } from "./theme";

/** What an audit covers; the whole page unless selectors narrow it. */
export interface AuditScope {
    include?: string | string[];
}

/**
 * Run the accessibility audit on the page's settled state.
 *
 * Axe reads whatever is rendered, and a state mid-transition is not a state: a pass started while
 * a hover or theme transition is in flight reads blended colors that match no settled styling —
 * the trap {@link settleTransitions} exists for. The wait makes the audit read what a reader
 * would. A test that has just clicked should also park the pointer (`page.mouse.move(0, 0)`) when
 * it means to audit the resting page rather than a hovered control.
 */
export const audit = async (page: Page, { include }: AuditScope = {}) => {
    await settleTransitions(page);
    let builder = new AxeBuilder({ page });
    for (const selector of typeof include === "string" ? [include] : (include ?? [])) {
        builder = builder.include(selector);
    }

    return builder.analyze();
};
