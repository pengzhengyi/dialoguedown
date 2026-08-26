import { copyToClipboard } from "./path-display";
import { showToast } from "./toast";

/**
 * Copy the text of any clicked element carrying `data-copy`, and confirm it with a toast.
 *
 * Delegated from a container rather than bound per element, so a table that re-renders its rows
 * keeps working without rewiring. `data-copy` is the whole contract: an element that wants to be
 * copyable says so, and does not need to know who is listening.
 */
export function wireClickToCopy(root: HTMLElement): void {
    root.addEventListener("click", (event) => {
        const target = (event.target as Element | null)?.closest<HTMLElement>("[data-copy]");
        const value = target?.dataset.copy;
        if (!value) return;
        void copyToClipboard(value).then(() => showToast(`Copied ${value}`));
    });
}
