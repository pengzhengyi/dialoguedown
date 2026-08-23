import type { Report } from "./model";

/** Where the served session pushes hot-reload and problem events. */
const EVENTS_URL = "/api/events";

/** Which document a problem event is about, so the client can route it to the right controller. */
export type ProblemTarget = "document" | "config";

/** Handlers for the served session's event stream. */
export interface ServerEventHandlers {
    /** A recompiled report was pushed (the document changed on disk). */
    onReload(report: Report): void;
    /** A recompiled report was pushed for an external configuration change. */
    onReloadConfig(report: Report): void;
    /**
     * A compile error or a missing/unreadable document — a message to surface. A disk-level
     * problem carries {@link ProblemTarget} so it can be routed to the matching controller.
     */
    onProblem(message: string, target?: ProblemTarget): void;
    /**
     * This tab's document stopped being the one served, because another was opened. Nothing more
     * will arrive on this stream, and the watch that fed it is gone.
     */
    onDisplaced(document: string): void;
}

/**
 * A live subscription to the served session's event stream, which belongs to **one** document:
 * the server binds a stream to whichever script was active when it opened.
 */
export interface ServerEventWatch {
    /** The stream currently connected. */
    readonly source: EventSource;
    /**
     * Reconnect against {@link document}, so events follow the script the reader has just opened.
     * A stream opened against the previous document keeps reporting on a session nobody is looking
     * at any more.
     */
    resubscribe(document: string | null): void;
}

/**
 * Subscribe to the served session's event stream. On each push it routes a `reload`
 * (a recompiled report from a document change), a `reload-config` (a recompiled report from an
 * external configuration change), or a `problem` (a message) to the handlers; the mode controller
 * decides what to do with a reload (View re-syncs, Edit chips). The browser's `EventSource`
 * reconnects on its own if the connection drops.
 */
export function watchServerEvents(
    handlers: ServerEventHandlers,
    document: string | null = null,
    createSource: (url: string) => EventSource = (url) => new EventSource(url),
): ServerEventWatch {
    let showing = document;
    let events = connect();

    function connect(): EventSource {
        // Naming the document binds the stream to the script this tab is showing. Without it the
        // server can only bind to whatever is active, which after a switch is somebody else's.
        const source = createSource(
            showing === null ? EVENTS_URL : `${EVENTS_URL}?doc=${encodeURIComponent(showing)}`,
        );

        source.addEventListener("reload", (event) => {
            handlers.onReload(JSON.parse((event as MessageEvent).data) as Report);
        });

        source.addEventListener("reload-config", (event) => {
            handlers.onReloadConfig(JSON.parse((event as MessageEvent).data) as Report);
        });

        source.addEventListener("problem", (event) => {
            const { message, target } = JSON.parse((event as MessageEvent).data) as {
                message: string;
                target?: ProblemTarget;
            };
            handlers.onProblem(message, target);
        });

        source.addEventListener("displaced", (event) => {
            // Closing matters: an `EventSource` reconnects a stream that merely ends, and the
            // reconnection would bind to whichever document is active now — this tab would start
            // applying another script's reloads to what it is showing.
            source.close();
            const { document: displaced } = JSON.parse((event as MessageEvent).data) as {
                document: string;
            };
            handlers.onDisplaced(displaced);
        });

        return source;
    }

    return {
        get source() {
            return events;
        },
        resubscribe(next: string | null) {
            showing = next;
            events.close();
            events = connect();
        },
    };
}
