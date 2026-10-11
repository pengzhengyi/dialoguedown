/**
 * @file A single, shared "toast" — a brief status message that fades in at the bottom of the
 * viewport and hides itself. One element is reused for every message (a second toast replaces the
 * first rather than stacking), which suits short confirmations like "Copied". It is announced
 * politely so a screen reader hears the confirmation too.
 */

const TOAST_CLASS = "toast";
const VISIBLE_CLASS = "visible";
const DEFAULT_DURATION_MS = 1400;

let toastEl: HTMLElement | null = null;
let hideTimer: ReturnType<typeof setTimeout> | undefined;

function ensureToast(host: HTMLElement): HTMLElement {
    if (toastEl?.isConnected) return toastEl;
    toastEl = document.createElement("div");
    toastEl.className = TOAST_CLASS;
    toastEl.setAttribute("role", "status");
    toastEl.setAttribute("aria-live", "polite");
    host.appendChild(toastEl);
    return toastEl;
}

/** Briefly show a message in the shared toast, replacing any message already showing. */
export function showToast(
    message: string,
    {
        durationMs = DEFAULT_DURATION_MS,
        host = document.body,
    }: { durationMs?: number; host?: HTMLElement } = {},
): void {
    const el = ensureToast(host);
    el.textContent = message;
    el.classList.add(VISIBLE_CLASS);
    clearTimeout(hideTimer);
    hideTimer = setTimeout(() => el.classList.remove(VISIBLE_CLASS), durationMs);
}
