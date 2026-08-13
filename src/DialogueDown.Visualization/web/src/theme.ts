/**
 * The reader's color-theme choice. "system" follows the OS (no override); "light" and
 * "dark" force the theme. The choice drives Pico's `data-theme` attribute (and the
 * editor's `--md-*` colors via CSS), and is remembered in `localStorage`.
 */
export type ThemePreference = "system" | "light" | "dark";

const STORAGE_KEY = "dd-theme";
const OPTIONS: ReadonlyArray<[ThemePreference, string]> = [
    ["system", "System"],
    ["light", "Light"],
    ["dark", "Dark"],
];

/** Read the stored preference, defaulting to "system" (and tolerating disabled storage). */
export function readThemePreference(storage: Storage = localStorage): ThemePreference {
    try {
        const value = storage.getItem(STORAGE_KEY);
        return value === "light" || value === "dark" ? value : "system";
    } catch {
        return "system";
    }
}

/**
 * Apply a preference: set (or clear, for "system") `data-theme` on the root element so
 * Pico and the editor colors follow, and persist the choice. Storage failures (e.g. a
 * `file://` report) are ignored — the theme still applies for the session.
 */
export function applyThemePreference(
    preference: ThemePreference,
    root: HTMLElement = document.documentElement,
    storage: Storage = localStorage,
): void {
    if (preference === "system") {
        root.removeAttribute("data-theme");
    } else {
        root.setAttribute("data-theme", preference);
    }
    try {
        if (preference === "system") {
            storage.removeItem(STORAGE_KEY);
        } else {
            storage.setItem(STORAGE_KEY, preference);
        }
    } catch {
        // storage unavailable (private mode / file://) — the applied theme still holds
    }
}

/**
 * Build the theme `<select>` (System / Light / Dark), reflecting the initial choice and
 * applying each change through {@link apply}.
 */
export function createThemeSelect(
    apply: (preference: ThemePreference) => void = applyThemePreference,
    initial: ThemePreference = readThemePreference(),
): HTMLSelectElement {
    const select = document.createElement("select");
    select.className = "theme-select";
    select.setAttribute("aria-label", "Color theme");
    select.title = "Color theme";
    for (const [value, label] of OPTIONS) {
        const option = document.createElement("option");
        option.value = value;
        option.textContent = label;
        select.append(option);
    }
    select.value = initial;
    select.addEventListener("change", () => apply(select.value as ThemePreference));
    return select;
}

/** Feather Icons (MIT), sized for the header. Line icons matching the GitHub glyph. */
const icon = (paths: string): string =>
    `<svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor"` +
    ` stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths}</svg>`;

const THEME_ICONS: Record<ThemePreference, string> = {
    light: icon(
        '<circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/>' +
            '<line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/>' +
            '<line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/>' +
            '<line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/>' +
            '<line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/>',
    ),
    dark: icon('<path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>'),
    system: icon(
        '<rect x="2" y="3" width="20" height="14" rx="2" ry="2"/>' +
            '<line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/>',
    ),
};

/** The icon for a preference: a sun (light), a moon (dark), or a monitor (system). */
export function themeIcon(preference: ThemePreference): string {
    return THEME_ICONS[preference];
}

/**
 * The header theme control: the {@link createThemeSelect} paired with a leading icon
 * (sun / moon / monitor) that reflects — and updates with — the current choice.
 */
export function createThemeControl(
    apply: (preference: ThemePreference) => void = applyThemePreference,
    initial: ThemePreference = readThemePreference(),
): HTMLElement {
    const control = document.createElement("div");
    control.className = "theme-control";

    const glyph = document.createElement("span");
    glyph.className = "theme-icon";
    glyph.setAttribute("aria-hidden", "true");
    glyph.innerHTML = themeIcon(initial);

    const select = createThemeSelect((preference) => {
        glyph.innerHTML = themeIcon(preference);
        apply(preference);
    }, initial);

    control.append(glyph, select);
    return control;
}

/**
 * Apply the stored preference, mount its control, and report every effective-theme change.
 * System changes arrive through the media query even though `data-theme` stays absent.
 */
export function initTheme(
    host: Element | null,
    onEffectiveThemeChange: () => void = () => undefined,
): () => void {
    let preference = readThemePreference();
    applyThemePreference(preference);
    host?.prepend(
        createThemeControl((next) => {
            preference = next;
            applyThemePreference(next);
            onEffectiveThemeChange();
        }, preference),
    );

    const media =
        typeof window.matchMedia === "function"
            ? window.matchMedia("(prefers-color-scheme: dark)")
            : null;
    const onSystemChange = (): void => {
        if (preference === "system") onEffectiveThemeChange();
    };
    media?.addEventListener("change", onSystemChange);
    return () => media?.removeEventListener("change", onSystemChange);
}
