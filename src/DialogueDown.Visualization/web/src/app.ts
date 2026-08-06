import type {
    Report,
    Stage,
    StageUnavailable,
    ConfigReport,
    LspDiagnostic,
    ReservedTarget,
    SemanticToken,
    DialogueSymbolProvider,
    DisplayNode,
} from "./model";
import { createDetailPanel } from "./detail-panel";
import { createTreeView, type TreeView } from "./tree-view";
import type { CameraTransform } from "./graph-camera";
import { GraphCameraStore } from "./graph-camera";
import { createSourceView, type SourceViewHandle } from "./source-view";
import { createConfigView, type ConfigViewHandle, type ConfigViewOptions } from "./config-view";
import { consumeOpenConfigTab } from "./config-create";
import { rememberActiveTab, rememberedActiveTab } from "./active-tab";
import { createSemanticView } from "./semantic-view";
import { initResizer } from "./resizer";
import { initFullscreen } from "./fullscreen";
import { installMaximizeControls } from "./maximize-controls";
import { initCollapsiblePanel } from "./collapse-toggle";
import { initTooltips, initTabTooltips } from "./tooltips";
import { isTextEntryTarget } from "./text-entry";
import { escapeHtml } from "./text";

const JUMP_AWARE_STAGE_TITLES = new Set(["Dialogue AST", "Desugared AST", "Semantic Model"]);
import { setHelp } from "./help";
import type { DebugController } from "./debug-controller";

// The Source tab shows the compiler input, not a projected stage, so its hover
// tip is a constant here rather than a field on the model.
const SOURCE_TIP = "The document as written, beside a live Markdown preview.";

// The Config tab shows the applied dialogue.toml, so its tip is a constant here too.
const CONFIG_TIP = "The applied configuration — its dialogue.toml beside the configured speakers.";

// Feather Icons (MIT) `settings` gear, marking the Config tab.
const GEAR_ICON =
    '<svg class="tab-icon" viewBox="0 0 24 24" width="14" height="14" fill="none"' +
    ' stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"' +
    ' aria-hidden="true"><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.65 1.65 0 0 0' +
    " .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1" +
    " 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0" +
    " 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65" +
    " 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0" +
    " 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0" +
    " 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2" +
    ' 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" /></svg>';

/** How the Source tab's editor is wired for a served session. */
export interface SourceOptions {
    /** Start editable (Edit) or read-only (View); toggled later via {@link AppController.setEditable}. */
    editable: boolean;
    /** Called with the new buffer on every editor change (edits, or a View-mode reload). */
    onChange(buffer: string): void;
    /** Called with the new config (TOML) buffer on every config-editor change. */
    configOnChange?(buffer: string): void;
    /**
     * Create a `dialogue.toml` for a project that has none — the Config tab's no-config call to
     * action (Edit only). Absent for the static export.
     */
    onCreateConfig?(): Promise<void>;
    /**
     * Where the Source editor's autocompletion draws its symbols — the compiler's resolved
     * symbols from the report payload. Absent for the static export (no completions).
     */
    symbols?: DialogueSymbolProvider;
    /**
     * An asynchronous boundary guarding navigation (switching tabs or selecting another node): it
     * runs `proceed` once the active document's Auto save has flushed, or the reader chose to
     * discard in Manual, and does nothing when navigation should stay put. Only the latest intent
     * runs. Absent means navigation is always allowed.
     */
    beginNavigation?(proceed: () => void): void;
    /** Called whenever the active tab changes, so the caller can reflect the active document's chrome. */
    onActiveTabChange?(): void;
}

/** Controls a running report: swap in fresh data, reconfigure the editor, or show a banner. */
export interface AppController {
    /** Replace only the graph tabs with recompiled stages, leaving the Source tab (editor) intact. */
    updateStages(stages: Stage[]): void;
    /** Switch the Source editor between editable (Edit) and read-only (View) in place. */
    setEditable(editable: boolean): void;
    /** Replace the Source buffer (a View-mode hot-reload), keeping the one editor instance. */
    setContent(source: string): void;
    /** Replace the Source editor's diagnostics overlay after a recompile (hot-reload or save). */
    setDiagnostics(diagnostics: readonly LspDiagnostic[]): void;
    /** Replace the Source editor's semantic-token highlighting after a recompile. */
    setSemanticTokens(tokens: readonly SemanticToken[]): void;
    /** Replace the fixed panel's language-owned jump targets after a recompile. */
    setReservedTargets(targets: readonly ReservedTarget[]): void;
    /** Switch the config (TOML) editor between editable (Edit) and read-only (View) in place. */
    setConfigEditable(editable: boolean): void;
    /** Replace the config editor's content — a discard/restore of the last saved TOML. */
    setConfigContent(source: string): void;
    /** Re-render the mode row and configured speakers after a config recompile. */
    updateConfig(config: ConfigReport): void;
    /** Mark the config speakers pane as stale (unsaved edits) or up to date. */
    setConfigStale(stale: boolean): void;
    /** Toggle the Source tab's unsaved (dirty) marker. */
    markSourceDirty(dirty: boolean): void;
    /** Toggle the Config tab's unsaved (dirty) marker. */
    markConfigDirty(dirty: boolean): void;
    /** Whether the Config tab is the active tab (so a save/⌘S targets the config). */
    isConfigTabActive(): boolean;
    /** Activate the Config tab — the Explorer's "open dialogue.toml"; a no-op without a config. */
    showConfigTab(): void;
    /** Show a status message (e.g. a live compile error), or clear it with `null`. */
    showBanner(message: string | null): void;
}

/**
 * Build the tabs — an optional Source tab followed by one per stage — wire the
 * shared interactions, and return a controller for live updates.
 */
export function runApp(
    report: Report,
    source?: SourceOptions,
    debug?: DebugController,
): AppController {
    // TODO(runtime-debugger, #45): Inject a server-backed DebugController here once the
    // dialogue graph and runtime can publish source-mapped execution snapshots. Until then,
    // production callers omit it and the debugger UI remains completely dormant.
    const tabsEl = document.getElementById("tabs")!;
    const stagesEl = document.getElementById("stages")!;
    const appEl = document.getElementById("app")!;
    const bannerEl = document.getElementById("live-banner")!;
    let activeIndex = 0;
    let sourcePresent = false;
    let configPresent = false;
    let sourceHandle: SourceViewHandle | null = null;
    let configHandle: ConfigViewHandle | null = null;
    let sourceTab: Element | null = null;
    let configTab: Element | null = null;

    // The node inspector is read-only: editing lives solely in the Source tab, and the panel's
    // "Jump to source" takes the reader there with the node's span selected.
    const panel = createDetailPanel(
        // A "Jump to source" is offered whenever there is a Source tab to land in — served or a
        // static export — since a read-only editor is still selectable.
        report.source != null ? { jumpToSource } : {},
    );

    // Jump from a selected graph node to its source in the Source tab: switch tabs (through the
    // save-safe navigation guard, so an Auto save flushes or a Manual prompt resolves first), then
    // select the node's span — or place the caret for a zero-width, synthetic node.
    function jumpToSource(span: { start: number; end: number }): void {
        if (!sourcePresent) return;
        const target = configPresent ? 1 : 0; // the Source tab sits after an optional Config tab
        const land = () => {
            if (!sourceHandle) return;
            activate(target);
            sourceHandle.selectRange(span.start, span.end);
        };
        if (source?.beginNavigation) source.beginNavigation(land);
        else land();
    }
    // Per tab: its tree view (graph tabs) or null (the Source tab, which has no
    // node-detail panel and no keyboard tree navigation).
    let views: (TreeView | null)[] = [];
    // Per tab: its camera-store key — the stage title for a graph tab, or null for
    // the Source tab (which has no graph and no camera).
    let keys: (string | null)[] = [];
    // Per tab: its title, the stable identifier used to remember the last-open tab so a
    // refresh returns to it (indices shift when the Config tab is present, titles do not).
    let titles: string[] = [];
    // Remembers each stage's zoom/pan and fold across tab switches and rebuilds.
    const cameras = new GraphCameraStore();
    // The stable id of the node the inspector is currently showing, or null when nothing is
    // selected. Captured before a save-triggered `updateStages` rebuild so the selection — and
    // the open inspector editor bound to it — can be restored against the freshly built view.
    let selectedNodeId: string | null = null;

    // Record the shown node's stable id, then drive the shared inspector. Wrapping `panel.show`
    // (rather than calling it directly) is what lets `updateStages` reselect the same node after
    // a rebuild, keeping the inspector editor open and rebound to the node's current source.
    function showNode(node: DisplayNode, recognizeJumps: boolean): void {
        selectedNodeId = node.id;
        panel.show(node, { recognizeJumps });
    }

    // The whole-window maximize mode (graphs and the source split) — one page-level action,
    // so it gets one app-level control (at the right end of the tab-nav row) plus the
    // `f` / Escape keys, rather than a copy in every tab. Wired once for the app's lifetime.
    const fullscreen = initFullscreen();
    const focusControls = installMaximizeControls(
        tabsEl.parentElement ?? appEl,
        appEl,
        fullscreen.toggle,
        fullscreen.toggleZen,
    );

    build(report);

    // One-time wiring that outlives a re-render (the containers persist).
    document.addEventListener("keydown", (event) => {
        // Buttons, form controls, and the editor own their keys; never also drive graph
        // navigation from them, or arrows would move the graph while the cursor moves and
        // Space would toggle a fold mid-type.
        const target = event.target;
        if (isTextEntryTarget(target) || (target instanceof Element && target.closest("button"))) {
            return;
        }
        views[activeIndex]?.handleKey(event);
    });
    initResizer();
    // The node-details inspector can be hidden to give the graph the full width. Its
    // toggle lives on the resize divider, doubling as the always-present re-open handle
    // once the panel is gone; the choice is remembered across reloads.
    const resizerEl = document.getElementById("resizer");
    if (resizerEl) {
        const inspector = initCollapsiblePanel({
            container: appEl,
            collapsedClass: "detail-collapsed",
            storageKey: "dd-inspector-collapsed",
            name: "inspector",
        });
        resizerEl.appendChild(inspector.button);
    }
    initTooltips(stagesEl);
    initTabTooltips(tabsEl);

    return {
        updateStages,
        setEditable: (next) => {
            sourceHandle?.setEditable(next);
        },
        setContent: (next) => sourceHandle?.setContent(next),
        setDiagnostics: (diagnostics) => sourceHandle?.setDiagnostics(diagnostics),
        setSemanticTokens: (tokens) => sourceHandle?.setSemanticTokens(tokens),
        setReservedTargets: (targets) => sourceHandle?.setReservedTargets(targets),
        setConfigEditable: (next) => configHandle?.setEditable(next),
        setConfigContent: (next) => configHandle?.setContent(next),
        updateConfig: (config) => configHandle?.updateConfig(config),
        setConfigStale: (stale) => configHandle?.setStale(stale),
        markSourceDirty: (dirty) => sourceTab?.classList.toggle("dirty", dirty),
        markConfigDirty: (dirty) => configTab?.classList.toggle("dirty", dirty),
        isConfigTabActive: () => configPresent && activeIndex === 0,
        showConfigTab: () => {
            // Mirror a Config-tab click: route through the save-safe navigation guard so an Auto
            // save flushes (or a Manual prompt resolves) before the tab changes.
            if (!configPresent || activeIndex === 0) return;
            if (source?.beginNavigation) source.beginNavigation(() => activate(0));
            else activate(0);
        },
        showBanner(message) {
            bannerEl.textContent = message ?? "";
            bannerEl.hidden = message === null;
        },
    };

    function build(report: Report): void {
        tabsEl.replaceChildren();
        stagesEl.replaceChildren();
        views = [];
        keys = [];
        titles = [];
        sourcePresent = report.source != null;
        configPresent = report.configuration != null;

        // The Config tab comes first (a gear icon marks it), but the report still opens on
        // Source below — Config is one click away for a reader who just wants the dialogue.
        if (report.configuration != null) {
            const section = document.createElement("section");
            section.className = "stage config-stage";
            configHandle = createConfigView(report.configuration, {
                editable: source?.editable ?? false,
                ...(source?.configOnChange ? { onChange: source.configOnChange } : {}),
                ...(source?.onCreateConfig ? { onCreateConfig: source.onCreateConfig } : {}),
            } satisfies ConfigViewOptions);
            section.appendChild(configHandle.element);
            addTab("Config", section, null, CONFIG_TIP, null, GEAR_ICON);
            configTab = tabsEl.lastElementChild;
        }

        if (report.source != null) {
            const section = document.createElement("section");
            section.className = "stage source-stage";
            sourceHandle = createSourceView(report.source, {
                ...(source ? { editable: source.editable, onChange: source.onChange } : {}),
                ...(source?.symbols ? { symbols: source.symbols } : {}),
                reservedTargets: report.symbols?.reservedTargets ?? [],
                ...(debug ? { debug } : {}),
            });
            sourceHandle.setDiagnostics(report.diagnostics ?? []);
            sourceHandle.setSemanticTokens(report.semanticTokens ?? []);
            section.appendChild(sourceHandle.element);
            addTab("Source", section, null, SOURCE_TIP, null);
            sourceTab = tabsEl.lastElementChild;
        }
        for (const stage of report.stages) {
            addStageTab(stage);
        }
        // The focus-mode controls only make sense with a tab to focus; the empty state (no
        // config, source, or stages) has none, so hide them there.
        const nothingToFocus = views.length === 0;
        focusControls.maximize.hidden = nothingToFocus;
        focusControls.zen.hidden = nothingToFocus;
        // Open on the tab the reader last had open (remembered across a refresh), else Source
        // when present (after the Config tab), else the first available tab — unless a
        // just-created config asked the reloaded page to land on the Config tab. A disabled
        // (unavailable) stage is never opened: neither the remembered tab nor the fallback.
        if (views.length > 0) {
            const openConfig = configPresent && consumeOpenConfigTab();
            const remembered = titles.indexOf(rememberedActiveTab() ?? "");
            const rememberedOk = remembered >= 0 && !isTabDisabled(remembered);
            const fallback = sourcePresent ? (configPresent ? 1 : 0) : firstAvailableIndex();
            activate(openConfig ? 0 : rememberedOk ? remembered : fallback);
        }
    }

    /** The first tab the reader can open, skipping disabled (unavailable) stages. */
    function firstAvailableIndex(): number {
        for (let i = 0; i < tabsEl.children.length; i++) if (!isTabDisabled(i)) return i;
        return 0;
    }

    // Replace only the graph tabs (on a Live Edit save), leaving the Source tab and its
    // editor — and the reader's cursor — untouched. Each graph's remembered camera and
    // fold are recorded live (as the reader adjusts them), so a rebuilt stage restores
    // its position from the store. The inspector's selected node is remembered by its stable
    // id and reselected against the freshly built view, so a successful autosave never closes
    // the open node inspector; if the node is gone from the recompiled graph, the selection
    // cancels safely and the inspector clears.
    function updateStages(stages: Stage[]): void {
        const reselectId = selectedNodeId;
        const keep = (configPresent ? 1 : 0) + (sourcePresent ? 1 : 0);
        while (tabsEl.children.length > keep) tabsEl.lastElementChild!.remove();
        while (stagesEl.children.length > keep) stagesEl.lastElementChild!.remove();
        views = views.slice(0, keep);
        keys = keys.slice(0, keep);
        titles = titles.slice(0, keep);
        for (const stage of stages) {
            addStageTab(stage);
        }
        if (views.length > 0) {
            activate(Math.min(activeIndex, views.length - 1));
            // `activate` clears every view's selection and the inspector; restore the remembered
            // node in the now-active view (re-opening and rebinding its inspector editor). A
            // missing id resolves to `false` and leaves the inspector cleared.
            if (reselectId !== null) views[activeIndex]?.selectById(reselectId);
        }
    }

    function addStageTab(stage: Stage): void {
        const section = document.createElement("section");
        section.className = "stage";
        if (stage.unavailable) {
            // The stage's artifact was not produced (a halted compile). Render a disabled tab
            // the reader cannot enter; its content is intentionally empty — surfacing the
            // diagnostics is a separate concern.
            addTab(stage.title, section, null, stage.description, stage.title, undefined, {
                reason: stage.unavailable.reason,
            });
            return;
        }
        // The Semantic tab has a different shape: a scene-tree graph beside stacked tables.
        // It is still a graph stage, so it reuses the tree view (camera memory, fold, full
        // screen) — only the surrounding layout differs.
        const isSemantic = stage.tables != null;
        const recognizeJumps = JUMP_AWARE_STAGE_TITLES.has(stage.title);
        if (isSemantic) section.classList.add("semantic-stage");
        let view: TreeView | null = null;
        try {
            const treeOptions = {
                initialCamera: cameras.cameraFor(stage.title),
                initialFold: cameras.foldFor(stage.title),
                onCameraChange: (transform: CameraTransform, byUser: boolean) =>
                    byUser
                        ? cameras.adjustCamera(stage.title, transform)
                        : cameras.noteCamera(transform),
                onFoldChange: (collapsed: string[]) => cameras.setFold(stage.title, collapsed),
                onRevert: () => cameras.reset(stage.title),
            };
            if (isSemantic) {
                const semantic = createSemanticView(
                    stage,
                    (node) => showNode(node, recognizeJumps),
                    treeOptions,
                    report.source != null ? jumpToSource : undefined,
                    recognizeJumps,
                );
                view = semantic.view;
                section.appendChild(semantic.element);
            } else {
                // A stage shows its own pinned camera, else the shared current one it
                // inherits, else the default framing; its fold is always its own. Reader
                // adjustments are recorded live through the callbacks above.
                view = createTreeView(stage, (node) => showNode(node, recognizeJumps), treeOptions);
                section.appendChild(view.svg);
                section.appendChild(view.legend);
                section.appendChild(view.controls);
            }
        } catch (error) {
            section.classList.add("error");
            section.textContent = `Failed to render stage: ${(error as Error).message}`;
        }
        addTab(stage.title, section, view, stage.description, stage.title);
    }

    function addTab(
        title: string,
        section: HTMLElement,
        view: TreeView | null,
        tip: string,
        key: string | null,
        icon?: string,
        unavailable?: StageUnavailable,
    ): void {
        const index = views.length;
        const tab = document.createElement("button");
        tab.className = "tab";
        tab.type = "button";
        if (icon) {
            tab.classList.add("tab-with-icon");
            tab.innerHTML = `${icon}<span class="tab-label">${escapeHtml(title)}</span>`;
        } else {
            tab.textContent = title;
        }
        if (unavailable) {
            // A disabled tab: grayed and non-navigable, its tooltip saying why the stage is
            // missing. `aria-disabled` (not the `disabled` attribute) keeps the hover tooltip
            // firing so the reader learns the reason.
            tab.classList.add("unavailable");
            tab.setAttribute("aria-disabled", "true");
            tab.setAttribute("data-tip", unavailable.reason);
        } else {
            tab.setAttribute("data-tip", tip);
        }
        // Switching tabs is navigation: route it through the async guard so an Auto save flushes
        // (or a Manual prompt resolves) before the tab changes and a stale graph is shown beside
        // unsaved edits. An unavailable stage cannot be entered at all.
        tab.addEventListener("click", () => {
            if (unavailable) return;
            if (index === activeIndex) return;
            if (source?.beginNavigation) {
                source.beginNavigation(() => activate(index));
            } else {
                activate(index);
            }
        });
        tabsEl.appendChild(tab);
        stagesEl.appendChild(section);
        views.push(view);
        keys.push(key);
        titles.push(title);
    }

    /** Whether the tab at `index` is a disabled (unavailable) stage the reader cannot enter. */
    function isTabDisabled(index: number): boolean {
        return tabsEl.children[index]?.getAttribute("aria-disabled") === "true";
    }

    function activate(index: number): void {
        activeIndex = index;
        rememberActiveTab(titles[index]);
        Array.from(tabsEl.children).forEach((el, i) => el.classList.toggle("active", i === index));
        Array.from(stagesEl.children).forEach((el, i) =>
            el.classList.toggle("active", i === index),
        );
        // The Source tab (no tree view) and the Semantic tab (its own tables) have no shared
        // node-detail inspector; hide it so their content takes the full width.
        const isSource = views[index] === null;
        const section = stagesEl.children[index] as HTMLElement | undefined;
        const isSemantic = section?.classList.contains("semantic-stage") ?? false;
        appEl.classList.toggle("no-detail", isSource || isSemantic);
        setHelp(isSource ? "source" : isSemantic ? "semantic" : "graph");
        // Frame the tab now that it is visible (a tree built while hidden had a
        // zero-size container). Applying its remembered position — instead of always
        // re-framing — keeps a stage spatially stable as the reader moves between tabs.
        revealView(index);
        for (const view of views) view?.clearSelection();
        panel.clear();
        selectedNodeId = null;
        source?.onActiveTabChange?.();
    }

    /**
     * Show a tab's position now that it is visible: its own pinned camera, the shared
     * current camera it inherits, or the default framing — plus its remembered fold.
     */
    function revealView(index: number): void {
        const view = views[index];
        const key = keys[index];
        if (!view || !key) return;
        view.applyView(cameras.cameraFor(key), cameras.foldFor(key));
    }
}
