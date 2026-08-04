/** The debugger states the Source UI renders. */
export type DebugStatus =
    "unavailable" | "ready" | "running" | "paused" | "awaiting-path" | "ended" | "stale";

/** One source-mapped point where execution may pause. Lines are one-based. */
export interface DebugLocation {
    id: string;
    line: number;
    from: number;
    to: number;
    label: string;
}

/** One labeled route leaving an execution point. */
export interface DebugPath {
    id: string;
    label: string;
    targetId: string;
}

/** A requested source-line breakpoint and whether the bound program can stop there. */
export interface BreakpointBinding {
    line: number;
    verified: boolean;
}

/** Which toolbar commands the controller currently accepts. */
export interface DebugControls {
    start: boolean;
    continue: boolean;
    stepOver: boolean;
    stop: boolean;
}

/** The immutable state one debugger UI render consumes. */
export interface DebugSnapshot {
    status: DebugStatus;
    location?: DebugLocation;
    paths: readonly DebugPath[];
    breakpoints: readonly BreakpointBinding[];
    controls: DebugControls;
    message?: string;
}

export type DebugListener = (snapshot: DebugSnapshot) => void;

/**
 * The UI-facing debugger seam. The exploration branch supplies an in-browser fake; a later
 * runtime adapter can implement the same commands and snapshots without changing CodeMirror.
 *
 * TODO(runtime-debugger, #45): Implement a server-backed adapter when graph traversal lands.
 * It should translate runtime commands/events into this contract rather than coupling the
 * CodeMirror UI to transport or graph types.
 */
export interface DebugController {
    snapshot(): DebugSnapshot;
    subscribe(listener: DebugListener): () => void;
    setBreakpoints(lines: readonly number[]): void;
    start(): void;
    continue(): void;
    stepOver(): void;
    choosePath(pathId: string): void;
    stop(): void;
    sourceChanged(): void;
}
