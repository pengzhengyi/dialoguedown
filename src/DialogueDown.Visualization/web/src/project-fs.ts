/**
 * Project filesystem primitives shared by the Explorer and the served-shell wiring: the browse
 * listing shape (`GET /api/browse`), the create outcome, the script extension, and the
 * root-relative path helpers. Kept apart from any one UI so both the tree and the shell reuse them.
 */

/** A directory listing under the project root, as returned by `GET /api/browse`. */
export interface BrowseListing {
    path: string;
    parent: string | null;
    directories: string[];
    sources: string[];
}

/** The required extension for a DialogueDown script (auto-appended when creating). */
export const SCRIPT_EXTENSION = ".dialogue.md";

/** The outcome of a create request. */
export type CreateOutcome =
    | { kind: "opened"; url: string }
    | { kind: "exists"; path: string }
    | { kind: "error"; message: string };

/** The last path segment of a root-relative path (a display label). */
export function leafName(path: string): string {
    const parts = path.split("/").filter(Boolean);
    return parts.length > 0 ? parts[parts.length - 1] : path;
}

/** The parent (root-relative) of a path — the empty string at the root. */
export function parentPath(path: string): string {
    const parts = path.split("/").filter(Boolean);
    parts.pop();
    return parts.join("/");
}
