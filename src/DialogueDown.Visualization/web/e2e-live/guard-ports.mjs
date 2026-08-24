import { fileURLToPath } from "node:url";
import { dirname } from "node:path";
import {
    LIVE_PORT,
    RENDER_ROOT_PORT,
    SHELL_PORT,
    LIVE_EDIT_PORT,
    CONFIG_EDIT_PORT,
    CONFIG_CREATE_PORT,
    CONFIG_ADOPT_PORT,
    CONFIG_ADOPT_INVALID_PORT,
    SEMANTIC_AUTOCOMPLETE_PORT,
    SWITCH_PORT,
} from "./fixture.mjs";

/**
 * Refuse to run against a server this checkout did not start.
 *
 * Locally `reuseExistingServer` is on, so Playwright adopts whatever already holds a fixture
 * port rather than starting its own. That is what makes the inner loop quick, and it is also a
 * trap: a server left behind by *another worktree* serves a different build, and the suite runs
 * against it silently. The failures that follow look like real regressions and are not.
 *
 * Every fixture server renders the absolute path of the document or tree it serves, so asking
 * the page what it is showing is enough to tell one checkout's server from another's.
 */
const FIXTURES = dirname(fileURLToPath(import.meta.url));

const PORTS = [
    LIVE_PORT,
    RENDER_ROOT_PORT,
    SHELL_PORT,
    LIVE_EDIT_PORT,
    CONFIG_EDIT_PORT,
    CONFIG_CREATE_PORT,
    CONFIG_ADOPT_PORT,
    CONFIG_ADOPT_INVALID_PORT,
    SEMANTIC_AUTOCOMPLETE_PORT,
    SWITCH_PORT,
];

async function servedBody(port) {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), 5_000);
    try {
        const response = await fetch(`http://127.0.0.1:${port}/`, {
            redirect: "follow",
            signal: controller.signal,
        });
        return await response.text();
    } catch {
        // Nothing listening, or nothing answering in time: Playwright starts its own server.
        return null;
    } finally {
        clearTimeout(timer);
    }
}

export default async function guardPorts() {
    return guardPortsIn(PORTS);
}

/**
 * The guard itself, over an explicit set of ports.
 *
 * Playwright hands `globalSetup` its own configuration, so the ports cannot be a default
 * parameter of the entry point above; taking them here also lets a test drive the guard over one
 * port it controls rather than over whatever the machine happens to be running.
 */
export async function guardPortsIn(ports, fixtures = FIXTURES) {
    const strangers = [];
    for (const port of ports) {
        const body = await servedBody(port);
        if (body !== null && !body.includes(fixtures)) strangers.push(port);
    }
    if (strangers.length === 0) return;

    const taken = strangers.join(", ");
    throw new Error(
        `Live E2E: ${taken} ${strangers.length === 1 ? "is" : "are"} held by a server that is ` +
            `not serving this checkout's fixtures (${fixtures}).\n` +
            "Playwright would reuse it and run this suite against a different build, so the " +
            "results would be meaningless. Stop the other server — most often one left running " +
            "by another worktree — and run again.",
    );
}
