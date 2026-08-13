import type { Mermaid, MermaidConfig, ParseOptions, ParseResult, RenderResult } from "mermaid";

const MAX_TEXT_SIZE = 50_000;
const SOURCE_SELECTOR = ".mermaid-source code";
const DIAGRAM_SELECTOR = "[data-mermaid]";

export interface MermaidApi {
    initialize(config: MermaidConfig): void;
    parse(text: string, options?: ParseOptions): Promise<ParseResult | false>;
    render(id: string, text: string): Promise<RenderResult>;
}

export type MermaidTheme = "light" | "dark";

interface MermaidPreviewDependencies {
    load?: () => Promise<MermaidApi>;
    theme?: () => MermaidTheme;
}

interface HostState {
    revision: number;
    timer: number | null;
}

export interface MermaidPreviewService {
    /** Render every Mermaid placeholder currently mounted under the host. */
    renderNow(host: HTMLElement): Promise<void>;
    /** Coalesce live edits, then render the host's latest revision. */
    schedule(host: HTMLElement, delay?: number): void;
    /** Re-render every connected host, used when the effective color theme changes. */
    rerenderAll(): Promise<void>;
    /** Forget a host and cancel its pending timer. */
    dispose(host: HTMLElement): void;
}

/** Build the browser-side Mermaid lifecycle around an injectable API for focused unit tests. */
export function createMermaidPreviewService(
    dependencies: MermaidPreviewDependencies = {},
): MermaidPreviewService {
    const load = dependencies.load ?? loadMermaid;
    const theme = dependencies.theme ?? effectiveTheme;
    const hosts = new Map<HTMLElement, HostState>();
    let apiPromise: Promise<MermaidApi> | null = null;
    let queue: Promise<void> = Promise.resolve();
    let nextDiagramId = 0;

    function stateOf(host: HTMLElement): HostState {
        const existing = hosts.get(host);
        if (existing) return existing;
        const created = { revision: 0, timer: null };
        hosts.set(host, created);
        return created;
    }

    function enqueue(task: () => Promise<void>): Promise<void> {
        const run = queue.then(task, task);
        queue = run.catch(() => undefined);
        return run;
    }

    function renderNow(host: HTMLElement): Promise<void> {
        const state = stateOf(host);
        state.revision++;
        if (state.timer !== null) {
            window.clearTimeout(state.timer);
            state.timer = null;
        }
        if (!host.querySelector(DIAGRAM_SELECTOR)) return Promise.resolve();
        return renderRevision(host, state, state.revision);
    }

    function schedule(host: HTMLElement, delay = 200): void {
        const state = stateOf(host);
        state.revision++;
        if (state.timer !== null) window.clearTimeout(state.timer);
        if (!host.querySelector(DIAGRAM_SELECTOR)) {
            state.timer = null;
            return;
        }
        const revision = state.revision;
        state.timer = window.setTimeout(() => {
            state.timer = null;
            void renderRevision(host, state, revision);
        }, delay);
    }

    function renderRevision(host: HTMLElement, state: HostState, revision: number): Promise<void> {
        return enqueue(async () => {
            if (!isCurrent(host, state, revision)) return;
            apiPromise ??= load();
            const api = await apiPromise;
            api.initialize(configFor(theme()));

            for (const diagram of host.querySelectorAll<HTMLElement>(DIAGRAM_SELECTOR)) {
                if (!isCurrent(host, state, revision)) return;
                await renderDiagram(api, diagram, host, state, revision);
            }
        });
    }

    async function renderDiagram(
        api: MermaidApi,
        diagram: HTMLElement,
        host: HTMLElement,
        state: HostState,
        revision: number,
    ): Promise<void> {
        const source =
            diagram.dataset.mermaidSource ??
            diagram.querySelector<HTMLElement>(SOURCE_SELECTOR)?.textContent ??
            "";
        diagram.dataset.mermaidSource = source;

        if (source.trim() === "") {
            showSource(diagram, source, "Empty Mermaid diagram.");
            return;
        }

        try {
            const parsed = await api.parse(source, { suppressErrors: true });
            if (parsed === false) {
                showSource(diagram, source, "Mermaid diagram has invalid syntax.");
                return;
            }
            const result = await api.render(`dd-mermaid-${++nextDiagramId}`, source);
            if (!isCurrent(host, state, revision) || !diagram.isConnected) return;
            diagram.innerHTML = result.svg;
            applyAccessibleFallback(diagram);
        } catch (error) {
            if (!isCurrent(host, state, revision) || !diagram.isConnected) return;
            showSource(
                diagram,
                source,
                error instanceof Error ? error.message : "Mermaid diagram could not be rendered.",
            );
        }
    }

    async function rerenderAll(): Promise<void> {
        const renders: Promise<void>[] = [];
        for (const host of hosts.keys()) {
            if (host.isConnected) renders.push(renderNow(host));
            else dispose(host);
        }
        await Promise.all(renders);
    }

    function dispose(host: HTMLElement): void {
        const state = hosts.get(host);
        if (state) {
            state.revision++;
            if (state.timer != null) window.clearTimeout(state.timer);
        }
        hosts.delete(host);
    }

    return { renderNow, schedule, rerenderAll, dispose };
}

export const mermaidPreviews = createMermaidPreviewService();

function isCurrent(host: HTMLElement, state: HostState, revision: number): boolean {
    return host.isConnected && state.revision === revision;
}

async function loadMermaid(): Promise<MermaidApi> {
    return (await import("mermaid")).default as Mermaid;
}

function effectiveTheme(): MermaidTheme {
    const preference = document.documentElement.dataset.theme;
    if (preference === "dark" || preference === "light") return preference;
    return window.matchMedia?.("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

function configFor(theme: MermaidTheme): MermaidConfig {
    const dark = theme === "dark";
    return {
        startOnLoad: false,
        securityLevel: "strict",
        maxTextSize: MAX_TEXT_SIZE,
        theme: "base",
        secure: [
            "securityLevel",
            "startOnLoad",
            "maxTextSize",
            "theme",
            "themeVariables",
            "themeCSS",
        ],
        themeVariables: dark
            ? {
                  darkMode: true,
                  background: "#111827",
                  primaryColor: "#1f2937",
                  primaryTextColor: "#e5e7eb",
                  primaryBorderColor: "#64748b",
                  lineColor: "#94a3b8",
                  secondaryColor: "#172033",
                  tertiaryColor: "#0f172a",
              }
            : {
                  darkMode: false,
                  background: "#ffffff",
                  primaryColor: "#f8fafc",
                  primaryTextColor: "#1f2937",
                  primaryBorderColor: "#94a3b8",
                  lineColor: "#64748b",
                  secondaryColor: "#eff6ff",
                  tertiaryColor: "#f8fafc",
              },
    };
}

function applyAccessibleFallback(diagram: HTMLElement): void {
    const svg = diagram.querySelector("svg");
    if (svg?.hasAttribute("aria-label") || svg?.hasAttribute("aria-labelledby")) {
        diagram.removeAttribute("role");
        diagram.removeAttribute("aria-label");
        return;
    }
    diagram.setAttribute("role", "img");
    diagram.setAttribute("aria-label", "Mermaid diagram");
}

function showSource(diagram: HTMLElement, source: string, message: string): void {
    diagram.removeAttribute("role");
    diagram.removeAttribute("aria-label");

    const pre = document.createElement("pre");
    pre.className = "mermaid-source";
    const code = document.createElement("code");
    code.textContent = source;
    pre.appendChild(code);

    const error = document.createElement("p");
    error.className = "mermaid-error";
    error.textContent = message;
    diagram.replaceChildren(pre, error);
}
