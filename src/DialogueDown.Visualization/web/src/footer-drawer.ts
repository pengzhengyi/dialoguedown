import { codicon } from "./codicon";

/**
 * The footer drawer: one bounded, dismissible surface at the bottom of the report that hosts
 * several named panels behind a tab bar — the shape of VS Code's bottom panel.
 *
 * It is one drawer rather than one per panel because the footer has a single edge to anchor to.
 * Two independent disclosures would each need the same hard-won behavior — a height bound, an
 * internal scroll, floating over the stage on a short window — and would fight over the same
 * space when both were open.
 */

/** One named panel the drawer can show. */
export interface DrawerPanel {
    /** Stable id used to open the panel and to key its tab. */
    readonly id: string;
    /** The tab's visible label. */
    readonly label: string;
    /** The panel's content, mounted once and shown or hidden as tabs change. */
    readonly body: HTMLElement;
}

export interface FooterDrawerOptions {
    /** The element the drawer renders into (hidden while closed). */
    readonly host: HTMLElement;
    readonly panels: readonly DrawerPanel[];
    /** Called whenever the drawer opens or closes, so an opener can reflect the state. */
    onToggle?(open: boolean): void;
}

export interface FooterDrawer {
    /**
     * Show `id`. Opening the panel already on screen closes the drawer, so a single control
     * toggles it; opening a different one switches without closing. `opener` is focused again
     * when the drawer closes.
     */
    open(id: string, opener?: HTMLElement): void;
    close(): void;
    isOpen(): boolean;
    /** The panel currently shown, or `null` while closed. */
    activePanel(): string | null;
}

export function createFooterDrawer(options: FooterDrawerOptions): FooterDrawer {
    const { host, panels } = options;
    host.classList.add("footer-drawer");

    const tabs = document.createElement("div");
    tabs.className = "drawer-tabs";
    tabs.setAttribute("role", "tablist");

    const close = document.createElement("button");
    close.type = "button";
    close.className = "drawer-close";
    close.setAttribute("aria-label", "Close panel");
    close.appendChild(codicon("chrome-close", "drawer-close-icon"));
    close.addEventListener("click", () => api.close());

    const bodies = document.createElement("div");
    bodies.className = "drawer-bodies";

    const tabFor = new Map<string, HTMLButtonElement>();
    for (const panel of panels) {
        const tab = document.createElement("button");
        tab.type = "button";
        tab.className = "drawer-tab";
        tab.dataset.panel = panel.id;
        tab.setAttribute("role", "tab");
        tab.textContent = panel.label;
        tab.addEventListener("click", () => show(panel.id));
        tabs.appendChild(tab);
        tabFor.set(panel.id, tab);

        panel.body.dataset.body = panel.id;
        panel.body.setAttribute("role", "tabpanel");
        bodies.appendChild(panel.body);
    }

    const bar = document.createElement("div");
    bar.className = "drawer-bar";
    bar.append(tabs, close);
    host.append(bar, bodies);

    let active: string | null = null;
    let opener: HTMLElement | null = null;

    /** Reflect `active` into the DOM: one visible body, one selected tab. */
    function render(): void {
        for (const panel of panels) {
            const selected = panel.id === active;
            panel.body.hidden = !selected;
            const tab = tabFor.get(panel.id)!;
            tab.setAttribute("aria-selected", String(selected));
            tab.classList.toggle("active", selected);
        }
        host.hidden = active === null;
    }

    function show(id: string): void {
        active = id;
        render();
    }

    const api: FooterDrawer = {
        open(id, from) {
            if (active === id) {
                api.close();
                return;
            }
            const wasOpen = active !== null;
            if (from) opener = from;
            show(id);
            if (!wasOpen) options.onToggle?.(true);
        },
        close() {
            if (active === null) return;
            active = null;
            render();
            options.onToggle?.(false);
            // Land the reader back on the control that opened the drawer: it is where they
            // expect to be, and it is how they reopen it.
            opener?.focus();
            opener = null;
        },
        isOpen: () => active !== null,
        activePanel: () => active,
    };

    render();
    return api;
}
