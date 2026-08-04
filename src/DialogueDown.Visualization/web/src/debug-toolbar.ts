import tippy, { type Instance } from "tippy.js";
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

interface IconControl {
    button: HTMLButtonElement;
    element: HTMLElement;
}

/** Build the compact Source-pane toolbar driven entirely by {@link DebugController} snapshots. */
export function createDebugToolbar(
    controller: DebugController,
    options: DebugToolbarOptions = {},
): DebugToolbar {
    const tips: Instance[] = [];
    const toolbar = document.createElement("div");
    toolbar.className = "dd-debug-toolbar";
    toolbar.setAttribute("role", "toolbar");
    toolbar.setAttribute("aria-label", "Line debugger");

    const controls = document.createElement("div");
    controls.className = "dd-debug-controls";

    const dragHandle = iconButton("grabber", "Move debugger panel", () => {}, tips);
    dragHandle.button.classList.add("dd-debug-drag-handle");
    const toggleBreakpoint = options.toggleBreakpoint
        ? iconButton(
              "debug-breakpoint",
              "Toggle breakpoint at cursor",
              options.toggleBreakpoint,
              tips,
          )
        : null;
    const start = iconButton("debug-start", "Start debugging", () => controller.start(), tips);
    const continueButton = iconButton(
        "debug-continue",
        "Continue",
        () => controller.continue(),
        tips,
    );
    const stepOver = iconButton("debug-step-over", "Step over", () => controller.stepOver(), tips);
    const stop = iconButton("debug-stop", "Stop debugging", () => controller.stop(), tips);
    if (toggleBreakpoint) controls.appendChild(toggleBreakpoint.element);
    controls.append(start.element, continueButton.element, stepOver.element, stop.element);

    const status = document.createElement("span");
    status.className = "dd-debug-status";
    status.setAttribute("aria-live", "polite");

    const label = document.createElement("span");
    label.className = "dd-debug-label";
    label.textContent = "Debugger";

    const row = document.createElement("div");
    row.className = "dd-debug-toolbar-row";
    row.append(dragHandle.element, controls, status, label);

    const paths = document.createElement("div");
    paths.className = "dd-debug-paths";
    paths.hidden = true;

    toolbar.append(row, paths);

    const render = (snapshot: DebugSnapshot): void => {
        start.button.disabled = !snapshot.controls.start;
        continueButton.button.disabled = !snapshot.controls.continue;
        stepOver.button.disabled = !snapshot.controls.stepOver;
        stop.button.disabled = !snapshot.controls.stop;
        status.textContent = statusText(snapshot);
        renderPaths(paths, snapshot, controller, () => {
            const target = [stepOver.button, continueButton.button, start.button, stop.button].find(
                (control) => !control.disabled,
            );
            target?.focus();
        });
    };

    render(controller.snapshot());
    const unsubscribe = controller.subscribe(render);
    const disposeDrag = makeDraggable(toolbar, dragHandle.button);
    return {
        element: toolbar,
        destroy() {
            unsubscribe();
            disposeDrag();
            for (const tip of tips) tip.destroy();
        },
    };
}

function iconButton(
    iconName: string,
    ariaLabel: string,
    onClick: () => void,
    tips: Instance[],
): IconControl {
    const wrapper = document.createElement("span");
    wrapper.className = "dd-debug-control-wrap";
    const button = document.createElement("button");
    button.type = "button";
    button.className = "dd-debug-control";
    button.setAttribute("aria-label", ariaLabel);
    button.append(codicon(iconName, "dd-debug-control-icon"));
    button.addEventListener("click", onClick);
    wrapper.appendChild(button);
    tips.push(tippy(wrapper, { content: ariaLabel, placement: "bottom", delay: [150, 0] }));
    return { button, element: wrapper };
}

function makeDraggable(panel: HTMLElement, handle: HTMLElement): () => void {
    let dragging = false;
    let offsetX = 0;
    let offsetY = 0;

    const onMouseDown = (event: MouseEvent): void => {
        if (event.button !== 0 || !panel.parentElement) return;
        const bounds = panel.getBoundingClientRect();
        offsetX = event.clientX - bounds.left;
        offsetY = event.clientY - bounds.top;
        dragging = true;
        panel.classList.add("dd-debug-dragging");
        document.body.style.userSelect = "none";
        event.preventDefault();
    };

    const onMouseMove = (event: MouseEvent): void => {
        const container = panel.parentElement;
        if (!dragging || !container) return;
        const parentBounds = container.getBoundingClientRect();
        const panelBounds = panel.getBoundingClientRect();
        const left = clamp(
            event.clientX - parentBounds.left - offsetX,
            4,
            Math.max(4, parentBounds.width - panelBounds.width - 4),
        );
        const top = clamp(
            event.clientY - parentBounds.top - offsetY,
            4,
            Math.max(4, parentBounds.height - panelBounds.height - 4),
        );
        panel.style.left = `${Math.round(left)}px`;
        panel.style.top = `${Math.round(top)}px`;
        panel.style.right = "auto";
        panel.style.transform = "none";
    };

    const onMouseUp = (): void => {
        dragging = false;
        panel.classList.remove("dd-debug-dragging");
        document.body.style.userSelect = "";
    };

    const resize = (): void => clampDebugPanel(panel);
    const observer = typeof ResizeObserver === "function" ? new ResizeObserver(resize) : null;
    queueMicrotask(() => {
        if (!panel.parentElement) return;
        observer?.observe(panel.parentElement);
        observer?.observe(panel);
    });
    handle.addEventListener("mousedown", onMouseDown);
    document.addEventListener("mousemove", onMouseMove);
    document.addEventListener("mouseup", onMouseUp);
    window.addEventListener("resize", resize);
    return () => {
        handle.removeEventListener("mousedown", onMouseDown);
        document.removeEventListener("mousemove", onMouseMove);
        document.removeEventListener("mouseup", onMouseUp);
        window.removeEventListener("resize", resize);
        observer?.disconnect();
        if (dragging) document.body.style.userSelect = "";
    };
}

/** Keep an explicitly positioned debugger palette inside its current Source pane. */
export function clampDebugPanel(panel: HTMLElement): void {
    const container = panel.parentElement;
    if (!container || panel.style.left === "" || panel.style.top === "") return;
    const parentBounds = container.getBoundingClientRect();
    const panelBounds = panel.getBoundingClientRect();
    if (parentBounds.width === 0 || parentBounds.height === 0) return;
    const left = clamp(
        Number.parseFloat(panel.style.left),
        4,
        Math.max(4, parentBounds.width - panelBounds.width - 4),
    );
    const top = clamp(
        Number.parseFloat(panel.style.top),
        4,
        Math.max(4, parentBounds.height - panelBounds.height - 4),
    );
    panel.style.left = `${Math.round(left)}px`;
    panel.style.top = `${Math.round(top)}px`;
    panel.style.right = "auto";
    panel.style.transform = "none";
}

function clamp(value: number, min: number, max: number): number {
    return Math.max(min, Math.min(max, value));
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
