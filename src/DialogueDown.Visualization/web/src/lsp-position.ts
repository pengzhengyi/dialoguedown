import type { EditorState } from "@codemirror/state";
import type { LspPosition } from "./model";

/**
 * Resolve a zero-based LSP position to a document offset in `state`, clamped inside the
 * buffer. Shared by the diagnostics overlay, the semantic-token highlighting, and the source
 * view, so all of them map the compiler's LSP positions onto the editor the same way.
 *
 * A line past the last one is a stale range (the buffer shrank since the compile); it clamps
 * to the very end so a marker still shows rather than jumping to the wrong line. The
 * character is clamped to the line's length for the same reason.
 */
export function positionToOffset(state: EditorState, position: LspPosition): number {
    const { doc } = state;
    if (position.line + 1 > doc.lines) return doc.length;
    const line = doc.line(Math.max(position.line + 1, 1));
    const character = Math.min(Math.max(position.character, 0), line.length);
    return line.from + character;
}
