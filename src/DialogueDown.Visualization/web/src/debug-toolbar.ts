import { codicon } from "./codicon";
import type { DebugController, DebugSnapshot } from "./debug-controller";

/** The mounted debugger controls and their subscription cleanup. */
export interface DebugToolbar {
    readonly element: HTMLElement;
    destroy(): void;
}

/** Editor-owned actions that complement the controller's execution commands. */
export interface DebugToolbarOptions {
    /** Toggle a requested breakpoint on the CodeMirror cursor's line. */
    toggleBreakpoint?: () => void;
}

/** Build the compact Source-pane toolbar driven entirely by {@link DebugController} snapshots. */
export function createDebugToolbar(
    controller: DebugController,
    options: DebugToolbarOptions = {},
): DebugToolbar {
    const toolbar = document.createElement("div");
    toolbar.className = "dd-debug-toolbar";
    toolbar.setAttribute("role", "toolbar");
    toolbar.setAttribute("aria-label", "Line debugger prototype");

    const controls = document.createElement("div");
    controls.className = "dd-debug-controls";

    const toggleBreakpoint = options.toggleBreakpoint
        ? controlButton(
              "debug-breakpoint",
              "Toggle breakpoint at cursor",
              "Breakpoint",
              options.toggleBreakpoint,
          )
        : null;
    const start = controlButton("debug-start", "Start debugging", "Start", () =>
        controller.start(),
    );
    const continueButton = controlButton("debug-continue", "Continue", "Continue", () =>
        controller.continue(),
    );
    const stepOver = controlButton("debug-step-over", "Step over", "Step Over", () =>
        controller.stepOver(),
    );
    const stop = controlButton("debug-stop", "Stop debugging", "Stop", () => controller.stop());
    if (toggleBreakpoint) controls.appendChild(toggleBreakpoint);
    controls.append(start, continueButton, stepOver, stop);

    const status = document.createElement("span");
    status.className = "dd-debug-status";
    status.setAttribute("aria-live", "polite");

    const prototype = document.createElement("span");
    prototype.className = "dd-debug-prototype";
    prototype.textContent = "Prototype · fake program";

    const row = document.createElement("div");
    row.className = "dd-debug-toolbar-row";
    row.append(controls, status, prototype);

    const paths = document.createElement("div");
    paths.className = "dd-debug-paths";
    paths.hidden = true;

    toolbar.append(row, paths);

    const render = (snapshot: DebugSnapshot): void => {
        start.disabled = !snapshot.controls.start;
        continueButton.disabled = !snapshot.controls.continue;
        stepOver.disabled = !snapshot.controls.stepOver;
        stop.disabled = !snapshot.controls.stop;
        status.textContent = statusText(snapshot);
        renderPaths(paths, snapshot, controller, () => {
            const target = [stepOver, continueButton, start, stop].find(
                (control) => !control.disabled,
            );
            target?.focus();
        });
    };

    render(controller.snapshot());
    const unsubscribe = controller.subscribe(render);
    return { element: toolbar, destroy: unsubscribe };
}

function controlButton(
    iconName: string,
    ariaLabel: string,
    label: string,
    onClick: () => void,
): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "dd-debug-control";
    button.setAttribute("aria-label", ariaLabel);
    button.append(codicon(iconName, "dd-debug-control-icon"), document.createTextNode(label));
    button.addEventListener("click", onClick);
    return button;
}

function renderPaths(
    container: HTMLElement,
    snapshot: DebugSnapshot,
    controller: DebugController,
    focusAfterChoice: () => void,
): void {
    container.replaceChildren();
    container.hidden = snapshot.status !== "awaiting-path";
    if (container.hidden) return;

    const label = document.createElement("strong");
    label.textContent = "Choose path";
    container.appendChild(label);
    for (const path of snapshot.paths) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "dd-debug-path";
        button.dataset.pathId = path.id;
        button.textContent = path.label;
        button.addEventListener("click", () => {
            controller.choosePath(path.id);
            queueMicrotask(focusAfterChoice);
        });
        container.appendChild(button);
    }
}

function statusText(snapshot: DebugSnapshot): string {
    switch (snapshot.status) {
        case "unavailable":
        case "stale":
            return (
                snapshot.message ??
                (snapshot.status === "stale" ? "Source changed." : "Unavailable")
            );
        case "ready":
            return "Ready";
        case "running":
            return "Running…";
        case "paused":
            return locationStatus("Paused", snapshot);
        case "awaiting-path":
            return locationStatus("Choose path", snapshot);
        case "ended":
            return snapshot.message ?? "Ended";
    }
}

function locationStatus(prefix: string, snapshot: DebugSnapshot): string {
    const location = snapshot.location ? ` · line ${snapshot.location.line}` : "";
    const message = snapshot.message ? ` · ${snapshot.message}` : "";
    return prefix + location + message;
}
