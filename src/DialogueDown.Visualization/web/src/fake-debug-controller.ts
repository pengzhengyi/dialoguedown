import type {
    BreakpointBinding,
    DebugController,
    DebugControls,
    DebugListener,
    DebugLocation,
    DebugPath,
    DebugSnapshot,
    DebugStatus,
} from "./debug-controller";

/** One location in the explicit prototype fixture, bound by an exact unique source line. */
export interface FakeDebugLocation {
    id: string;
    anchor: string;
    label: string;
    paths: readonly DebugPath[];
}

/** The branch-only fake execution graph. It does not represent DialogueDown runtime semantics. */
export interface FakeDebugProgram {
    id: string;
    entryId: string;
    locations: readonly FakeDebugLocation[];
}

/** The fake controller's spike-only lifecycle seam. */
export interface FakeDebugController extends DebugController {
    rebind(source: string): void;
}

interface BoundProgram {
    entryId: string;
    locations: ReadonlyMap<string, DebugLocation>;
    paths: ReadonlyMap<string, readonly DebugPath[]>;
}

const NO_PATHS: readonly DebugPath[] = [];

/** Bind an explicit fixture and return the deterministic in-browser debugger used by the spike. */
export function createFakeDebugController(
    source: string,
    program: FakeDebugProgram,
): FakeDebugController {
    const listeners = new Set<DebugListener>();
    let bound = bindProgram(source, program);
    let status: DebugStatus = bound?.locations.has(bound.entryId) ? "ready" : "unavailable";
    let currentId: string | undefined;
    let pendingPaths: readonly DebugPath[] = NO_PATHS;
    let requestedLines: number[] = [];
    let message = status === "unavailable" ? unavailableMessage(program) : undefined;

    const emit = (): void => {
        const next = snapshot();
        for (const listener of listeners) listener(next);
    };

    const setState = (
        nextStatus: DebugStatus,
        options: {
            locationId?: string;
            paths?: readonly DebugPath[];
            message?: string;
        } = {},
    ): void => {
        status = nextStatus;
        currentId = options.locationId;
        pendingPaths = options.paths ?? NO_PATHS;
        message = options.message;
        emit();
    };

    const location = (id = currentId): DebugLocation | undefined =>
        id === undefined ? undefined : bound?.locations.get(id);

    const pathsFrom = (id = currentId): readonly DebugPath[] =>
        id === undefined ? NO_PATHS : (bound?.paths.get(id) ?? NO_PATHS);

    const moveTo = (targetId: string): DebugLocation | undefined => bound?.locations.get(targetId);

    const failMissingTarget = (targetId: string): void => {
        setState("ended", { message: `Prototype path target not found: ${targetId}` });
    };

    function snapshot(): DebugSnapshot {
        const current = location();
        return {
            status,
            ...(current ? { location: current } : {}),
            paths: pendingPaths,
            breakpoints: breakpointBindings(status, requestedLines, bound),
            controls: controlsFor(status),
            ...(message ? { message } : {}),
        };
    }

    return {
        snapshot,
        subscribe(listener) {
            listeners.add(listener);
            return () => listeners.delete(listener);
        },
        setBreakpoints(lines) {
            requestedLines = [...new Set(lines.filter(isSourceLine))].sort((a, b) => a - b);
            emit();
        },
        start() {
            if ((status !== "ready" && status !== "ended") || !bound) return;
            const entry = bound.locations.get(bound.entryId);
            if (!entry) {
                setState("unavailable", { message: unavailableMessage(program) });
                return;
            }
            setState("paused", { locationId: entry.id });
        },
        continue() {
            if (status !== "paused" || !bound || currentId === undefined) return;

            const firstPaths = pathsFrom();
            if (firstPaths.length > 1) {
                setState("awaiting-path", { locationId: currentId, paths: firstPaths });
                return;
            }

            status = "running";
            pendingPaths = NO_PATHS;
            message = undefined;
            emit();

            const visited = new Set([currentId]);
            while (currentId !== undefined) {
                const paths = pathsFrom();
                if (paths.length === 0) {
                    setState("ended");
                    return;
                }
                if (paths.length > 1) {
                    setState("awaiting-path", { locationId: currentId, paths });
                    return;
                }

                const target = moveTo(paths[0].targetId);
                if (!target) {
                    failMissingTarget(paths[0].targetId);
                    return;
                }
                currentId = target.id;

                if (isVerifiedBreakpoint(target.line, requestedLines)) {
                    setState("paused", { locationId: target.id });
                    return;
                }
                if (visited.has(target.id)) {
                    setState("paused", {
                        locationId: target.id,
                        message:
                            "Cycle encountered — step, choose another path, or set a breakpoint.",
                    });
                    return;
                }
                visited.add(target.id);

                const targetPaths = pathsFrom(target.id);
                if (targetPaths.length > 1) {
                    setState("awaiting-path", {
                        locationId: target.id,
                        paths: targetPaths,
                    });
                    return;
                }
            }
        },
        stepOver() {
            if (status !== "paused" || currentId === undefined) return;
            const paths = pathsFrom();
            if (paths.length === 0) {
                setState("ended");
                return;
            }
            if (paths.length > 1) {
                setState("awaiting-path", { locationId: currentId, paths });
                return;
            }
            const target = moveTo(paths[0].targetId);
            if (!target) {
                failMissingTarget(paths[0].targetId);
                return;
            }
            setState("paused", { locationId: target.id });
        },
        choosePath(pathId) {
            if (status !== "awaiting-path") return;
            const chosen = pendingPaths.find((path) => path.id === pathId);
            if (!chosen) return;
            const target = moveTo(chosen.targetId);
            if (!target) {
                failMissingTarget(chosen.targetId);
                return;
            }
            setState("paused", { locationId: target.id });
        },
        stop() {
            if (!controlsFor(status).stop) return;
            setState(bound?.locations.has(bound.entryId) ? "ready" : "unavailable", {
                message: bound?.locations.has(bound.entryId)
                    ? undefined
                    : unavailableMessage(program),
            });
        },
        sourceChanged() {
            if (status === "unavailable" || status === "stale") return;
            setState("stale", { message: "Source changed — save and restart." });
        },
        rebind(nextSource) {
            bound = bindProgram(nextSource, program);
            const ready = bound.locations.has(bound.entryId);
            setState(ready ? "ready" : "unavailable", {
                message: ready ? undefined : unavailableMessage(program),
            });
        },
    };
}

function bindProgram(source: string, program: FakeDebugProgram): BoundProgram {
    const lines = source.split(/\r?\n/);
    const starts: number[] = [];
    let offset = 0;
    for (const line of lines) {
        starts.push(offset);
        offset += line.length + 1;
    }

    const locations = new Map<string, DebugLocation>();
    const paths = new Map<string, readonly DebugPath[]>();
    for (const spec of program.locations) {
        const matches: number[] = [];
        for (let index = 0; index < lines.length; index += 1) {
            if (lines[index] === spec.anchor) matches.push(index);
        }
        if (matches.length !== 1) continue;
        const index = matches[0];
        const from = starts[index];
        locations.set(spec.id, {
            id: spec.id,
            line: index + 1,
            from,
            to: from + lines[index].length,
            label: spec.label,
        });
        paths.set(spec.id, spec.paths);
    }
    return { entryId: program.entryId, locations, paths };
}

function breakpointBindings(
    status: DebugStatus,
    lines: readonly number[],
    program: BoundProgram | null,
): BreakpointBinding[] {
    const executableLines =
        status === "stale" || status === "unavailable" || !program
            ? new Set<number>()
            : new Set([...program.locations.values()].map((location) => location.line));
    return lines.map((line) => ({ line, verified: executableLines.has(line) }));
}

function isVerifiedBreakpoint(line: number, requested: readonly number[]): boolean {
    return requested.includes(line);
}

function isSourceLine(line: number): boolean {
    return Number.isInteger(line) && line > 0;
}

function controlsFor(status: DebugStatus): DebugControls {
    return {
        start: status === "ready" || status === "ended",
        continue: status === "paused",
        stepOver: status === "paused",
        stop:
            status === "running" ||
            status === "paused" ||
            status === "awaiting-path" ||
            status === "ended",
    };
}

function unavailableMessage(program: FakeDebugProgram): string {
    return `Prototype fixture unavailable: ${program.id}`;
}
