/**
 * @file Whether an edited document saves on its own after a pause or only when asked, remembered
 * for each kind of document.
 */

/** When a dirty buffer is scheduled to save: `auto` after idle, or `manual` only on request. */
export type SaveMode = "auto" | "manual";

/** The two editable document types, each with its own persisted save-mode preference. */
export type DocumentType = "source" | "config";

/** The cookie name each document type persists its {@link SaveMode} under. */
const COOKIE_NAME: Record<DocumentType, string> = {
    source: "dd-save-mode-source",
    config: "dd-save-mode-config",
};

/**
 * Each document type's default {@link SaveMode}: Source starts Auto (the primary writer
 * surface), while Config starts Manual because transient TOML is often invalid mid-edit.
 */
const DEFAULT_MODE: Record<DocumentType, SaveMode> = {
    source: "auto",
    config: "manual",
};

/**
 * The cookie lifetime — ~400 days, the longest a modern browser honors — so a document type's
 * Auto/Manual choice survives well beyond a single report session.
 */
const MAX_AGE_SECONDS = 60 * 60 * 24 * 400;

/** Reads and persists one document type's {@link SaveMode} preference. */
export interface SaveModeStore {
    /** The persisted mode, or this document type's default when none is stored. */
    get(): SaveMode;
    /** Persist a new mode for this document type. */
    set(mode: SaveMode): void;
}

/**
 * A minimal cookie accessor, abstracting `document.cookie` so the store is testable and so an
 * environment without cookies degrades gracefully.
 */
export interface CookieJar {
    /** Every stored cookie serialized as `name=value; name=value`. */
    read(): string;
    /** Assign one cookie with a full `name=value; attributes` string, as `document.cookie` accepts. */
    write(cookie: string): void;
}

function isSaveMode(value: string | undefined): value is SaveMode {
    return value === "auto" || value === "manual";
}

/** Find one cookie's value in a serialized `name=value; name=value` string, ignoring the rest. */
function readCookie(jar: CookieJar, name: string): string | undefined {
    for (const pair of jar.read().split(";")) {
        const eq = pair.indexOf("=");
        if (eq === -1) continue;
        if (pair.slice(0, eq).trim() === name) return pair.slice(eq + 1).trim();
    }
    return undefined;
}

/**
 * A {@link SaveModeStore} backed by a host-scoped cookie, so a document type's Auto/Manual choice
 * survives across report sessions — and, unlike `localStorage`, across the ephemeral port each live
 * server binds (cookies are keyed by host, not by origin+port). The value is a non-sensitive
 * `auto`/`manual` flag written `SameSite=Strict` with a long `Max-Age`. Reads fall back to the
 * document type's default when nothing is stored or cookies are unavailable; writes swallow errors
 * (a blocked cookie jar just means the choice is not remembered, never a crash).
 */
export function createSaveModeStore(
    type: DocumentType,
    jar: CookieJar | null = safeCookieJar(),
): SaveModeStore {
    const name = COOKIE_NAME[type];
    const fallback = DEFAULT_MODE[type];
    return {
        get() {
            if (jar === null) return fallback;
            try {
                const stored = readCookie(jar, name);
                return isSaveMode(stored) ? stored : fallback;
            } catch {
                return fallback;
            }
        },
        set(mode) {
            if (jar === null) return;
            try {
                jar.write(`${name}=${mode}; Max-Age=${MAX_AGE_SECONDS}; Path=/; SameSite=Strict`);
            } catch {
                // A blocked cookie jar just means the choice is not remembered — not fatal.
            }
        },
    };
}

/** The default document mode, exposed for tests and callers that need it without a store. */
export function defaultSaveMode(type: DocumentType): SaveMode {
    return DEFAULT_MODE[type];
}

function safeCookieJar(): CookieJar | null {
    try {
        if (typeof document === "undefined") return null;
        // Touch the accessor once so an environment where cookies throw fails here, not later.
        void document.cookie;
        return {
            read: () => document.cookie,
            write: (cookie) => {
                document.cookie = cookie;
            },
        };
    } catch {
        return null;
    }
}
