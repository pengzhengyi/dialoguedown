import { codicon } from "./codicon";

/** A leaf entry in a right-click context menu: a codicon, a label, and the action it runs. */
export interface ContextMenuAction {
    icon: string;
    label: string;
    run: () => void;
}

/** A parent entry whose codicon and label open a nested {@link ContextMenuItem} submenu. */
export interface ContextMenuSubmenu {
    icon: string;
    label: string;
    submenu: readonly ContextMenuItem[];
}

/** One entry in a context menu — either an action or a submenu that nests more entries. */
export type ContextMenuItem = ContextMenuAction | ContextMenuSubmenu;

function isSubmenu(item: ContextMenuItem): item is ContextMenuSubmenu {
    return "submenu" in item;
}

// Only one context menu is open at a time across the whole app (Explorer rows, the editor), so the
// dismiss handle lives at module scope and opening a new menu closes any current one.
let dismissActive: (() => void) | null = null;

function dismiss(): void {
    dismissActive?.();
    dismissActive = null;
}

/**
 * Open a VS Code-style context menu at the cursor listing {@link items}. A {@link ContextMenuSubmenu}
 * entry opens a nested flyout to its side. The menu is dismissed by choosing a leaf action, pressing
 * Escape, or clicking elsewhere; `ArrowRight`/`Enter` open a submenu and `ArrowLeft` closes it.
 * Shared by the Explorer's row menus and the editor's surround and Jump-to menus.
 */
export function openContextMenu(event: MouseEvent, items: readonly ContextMenuItem[]): void {
    event.preventDefault();
    dismiss();

    const doc = document;
    // The open menus, root first; a submenu pushes a level, closing it pops back.
    const levels: { el: HTMLElement; parentItem: HTMLElement | null }[] = [];
    const openers = new WeakMap<HTMLElement, () => void>();

    const itemsOf = (menu: HTMLElement): HTMLElement[] => [
        ...menu.querySelectorAll<HTMLElement>(':scope > [role="menuitem"]'),
    ];

    function buildMenu(list: readonly ContextMenuItem[]): HTMLElement {
        const menu = doc.createElement("div");
        menu.className = "context-menu";
        menu.setAttribute("role", "menu");
        menu.setAttribute("aria-label", "Actions");
        for (const entry of list) {
            menu.append(isSubmenu(entry) ? submenuItem(entry) : actionItem(entry));
        }
        return menu;
    }

    function actionItem(entry: ContextMenuAction): HTMLButtonElement {
        const button = itemButton(entry.icon, entry.label);
        button.addEventListener("click", () => {
            dismiss();
            entry.run();
        });
        return button;
    }

    function submenuItem(entry: ContextMenuSubmenu): HTMLButtonElement {
        const button = itemButton(entry.icon, entry.label);
        button.setAttribute("aria-haspopup", "menu");
        button.setAttribute("aria-expanded", "false");
        button.append(codicon("chevron-right", "context-menu-chevron"));
        const open = (): void => openSubmenu(entry, button);
        openers.set(button, open);
        button.addEventListener("click", open);
        button.addEventListener("mouseenter", open);
        return button;
    }

    function levelIndexContaining(node: Node): number {
        for (let i = levels.length - 1; i >= 0; i--) {
            if (levels[i].el.contains(node)) return i;
        }
        return levels.length - 1;
    }

    function closeDeeperThan(index: number): void {
        while (levels.length - 1 > index) {
            const level = levels.pop();
            level?.parentItem?.setAttribute("aria-expanded", "false");
            level?.el.remove();
        }
    }

    function openSubmenu(entry: ContextMenuSubmenu, parentItem: HTMLElement): void {
        closeDeeperThan(levelIndexContaining(parentItem));
        const child = buildMenu(entry.submenu);
        child.classList.add("context-submenu");
        parentItem.setAttribute("aria-expanded", "true");
        doc.body.append(child);
        placeSubmenu(child, parentItem);
        levels.push({ el: child, parentItem });
        itemsOf(child)[0]?.focus();
    }

    function placeSubmenu(child: HTMLElement, parentItem: HTMLElement): void {
        const anchor = parentItem.getBoundingClientRect();
        let left = anchor.right - 2;
        let top = anchor.top;
        const win = doc.defaultView;
        if (win) {
            if (left + child.offsetWidth > win.innerWidth - 8)
                left = anchor.left - child.offsetWidth + 2;
            top = Math.min(top, Math.max(8, win.innerHeight - child.offsetHeight - 8));
        }
        child.style.left = `${Math.max(8, left)}px`;
        child.style.top = `${Math.max(8, top)}px`;
    }

    const onPointerDown = (e: Event): void => {
        if (!levels.some((level) => level.el.contains(e.target as Node))) dismiss();
    };
    const onKeyDown = (e: KeyboardEvent): void => {
        if (levels.length === 0) return;
        const active = doc.activeElement as HTMLElement | null;
        if (e.key === "Escape") {
            e.preventDefault();
            dismiss();
        } else if (e.key === "ArrowDown" || e.key === "ArrowUp") {
            e.preventDefault();
            const menu = levels[levelIndexContaining(active ?? levels[0].el)].el;
            const all = itemsOf(menu);
            const index = all.indexOf(active as HTMLElement);
            const step = e.key === "ArrowDown" ? 1 : -1;
            all[(index + step + all.length) % all.length]?.focus();
        } else if ((e.key === "ArrowRight" || e.key === "Enter") && active && openers.has(active)) {
            e.preventDefault();
            openers.get(active)?.();
        } else if (e.key === "ArrowLeft" && levels.length > 1) {
            e.preventDefault();
            const level = levels.pop();
            level?.el.remove();
            level?.parentItem?.setAttribute("aria-expanded", "false");
            level?.parentItem?.focus();
        }
    };

    doc.addEventListener("pointerdown", onPointerDown, true);
    doc.addEventListener("keydown", onKeyDown, true);
    dismissActive = (): void => {
        for (const level of levels) level.el.remove();
        levels.length = 0;
        doc.removeEventListener("pointerdown", onPointerDown, true);
        doc.removeEventListener("keydown", onKeyDown, true);
    };

    const root = buildMenu(items);
    levels.push({ el: root, parentItem: null });
    root.style.left = `${event.clientX}px`;
    root.style.top = `${event.clientY}px`;
    doc.body.append(root);
    // Keep the root within the viewport when opened near the right or bottom edge.
    const win = doc.defaultView;
    if (win) {
        const overflowX = event.clientX + root.offsetWidth - win.innerWidth + 8;
        const overflowY = event.clientY + root.offsetHeight - win.innerHeight + 8;
        if (overflowX > 0) root.style.left = `${event.clientX - overflowX}px`;
        if (overflowY > 0) root.style.top = `${event.clientY - overflowY}px`;
    }
    itemsOf(root)[0]?.focus();
}

// A context-menu entry button: an icon and a label, VS Code style.
function itemButton(iconName: string, label: string): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "context-menu-item";
    button.setAttribute("role", "menuitem");
    const text = document.createElement("span");
    text.className = "context-menu-label";
    text.textContent = label;
    button.append(codicon(iconName, "context-menu-icon"), text);
    return button;
}
