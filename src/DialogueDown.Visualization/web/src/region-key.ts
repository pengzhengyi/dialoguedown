/**
 * Naming an ignored region by what it contains.
 *
 * A reader's fold choice has to follow its region through a re-render — the Preview rebuilds its
 * whole document on every keystroke — so a region needs a name that is not its position. Inserting
 * a line above a region would otherwise hand its choice to an unrelated neighbor.
 *
 * The Source editor and the Preview keep separate fold state, but they name regions the same way,
 * so the two panes stay describable in one sentence.
 */

/** Names one region from its content, keeping identical siblings apart by the order they appear. */
export type RegionKey = (content: string) => string;

/** A fresh naming pass over one document. Occurrence counting restarts with each call. */
export function createRegionKeys(): RegionKey {
    const seen = new Map<string, number>();
    return (content) => {
        const digest = hashContent(content);
        const occurrence = seen.get(digest) ?? 0;
        seen.set(digest, occurrence + 1);
        return `${digest}:${occurrence}`;
    };
}

// FNV-1a. A collision would only let one region inherit another's fold choice until the next
// command that folds everything, so a short non-cryptographic digest is enough.
function hashContent(content: string): string {
    let hash = 0x811c9dc5;
    for (let index = 0; index < content.length; index += 1) {
        hash ^= content.charCodeAt(index);
        hash = Math.imul(hash, 0x01000193);
    }
    return (hash >>> 0).toString(36);
}
