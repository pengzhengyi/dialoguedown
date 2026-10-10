/**
 * @file Creates a `dialogue.toml` for a project that has none, then reloads the report on its
 * Config tab.
 */

const CREATE_CONFIG_URL = "/api/create-config";
const OPEN_CONFIG_KEY = "dd-open-config-after-create";

/** The side effects the create-config flow drives, injected so the logic is testable. */
export interface ConfigCreatePorts {
    /** POST the create request to the server; resolves to its response. */
    post(): Promise<Response>;
    /** Reload the page — the reloaded report is compiled with the new configuration. */
    reload(): void;
}

/**
 * How a successful create-config settled: a starter `dialogue.toml` was `created`, or a different
 * pre-existing file was `adopted` as recovery. Both open the Config tab after the reload; the
 * distinction lets a caller phrase the notice differently.
 */
export type ConfigCreateOutcome = "created" | "adopted";

/**
 * Create a `dialogue.toml` for a project that has none. On success it flags the reloaded page
 * to open on the Config tab and reloads (the reloaded report carries the configuration, so the
 * Config tab shows the new file in its editor). When a different config already exists the
 * server adopts it without overwriting rather than failing, so this still succeeds — reported as
 * `adopted` — and the reloaded page opens the existing Config. Only a genuine failure (a write
 * error, or a retry of an already-adopted file that diverged) throws with a reader-facing message.
 */
export async function createConfig(ports: ConfigCreatePorts): Promise<ConfigCreateOutcome> {
    const response = await ports.post();
    if (response.ok) {
        const outcome = await readOutcome(response);
        rememberOpenConfigTab();
        ports.reload();
        return outcome;
    }
    throw new Error(await errorMessage(response));
}

/** The production ports: POST the route and reload through `window.location`. */
export function browserConfigCreatePorts(): ConfigCreatePorts {
    return {
        post: () => fetch(CREATE_CONFIG_URL, { method: "POST" }),
        reload: () => window.location.reload(),
    };
}

/**
 * Whether a just-created config asked the reloaded page to open on the Config tab. Reads the
 * one-shot flag and clears it, so only the reload right after a create lands on Config.
 */
export function consumeOpenConfigTab(): boolean {
    try {
        if (window.sessionStorage.getItem(OPEN_CONFIG_KEY) === "1") {
            window.sessionStorage.removeItem(OPEN_CONFIG_KEY);
            return true;
        }
    } catch {
        // A blocked sessionStorage just means we open on the default tab — not fatal.
    }
    return false;
}

async function errorMessage(response: Response): Promise<string> {
    const fallback = "The configuration file could not be created.";
    try {
        const body = (await response.json()) as { message?: string };
        return body.message ?? fallback;
    } catch {
        // A blocked or empty body just falls back to the generic message.
    }
    return fallback;
}

// A `dialogue.toml` that already existed and was adopted comes back with an `adopted*` outcome; a
// freshly written starter comes back as `saved`. Anything unreadable is treated as a plain create.
async function readOutcome(response: Response): Promise<ConfigCreateOutcome> {
    try {
        const body = (await response.json()) as { outcome?: string };
        return body.outcome?.startsWith("adopted") ? "adopted" : "created";
    } catch {
        return "created";
    }
}

function rememberOpenConfigTab(): void {
    try {
        window.sessionStorage.setItem(OPEN_CONFIG_KEY, "1");
    } catch {
        // A blocked sessionStorage just means we open on the default tab — not fatal.
    }
}
