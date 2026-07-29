import { codicon } from "./codicon";

/** One entry in a right-click context menu: a codicon, a label, and the action it runs. */
export interface ContextMenuItem {
    icon: string;
    label: string;
    run: () => void;
}

// Only one context menu is open at a time across the whole app (Explorer rows, the editor), so the
// dismiss handle lives at module scope and opening a new menu closes any current one.
let dismissActive: (() => void) | null = null;

function dismiss(): void {
    dismissActive?.();
    dismissActive = null;
}

/**
 * Open a VS Code-style context menu at the cursor listing {@link items}. It is dismissed by
 * choosing an item, pressing Escape, or clicking elsewhere, and stays within the viewport near an
 * edge. Shared by the Explorer's row menus and the editor's surround menu.
 */
export function openContextMenu(event: MouseEvent, items: readonly ContextMenuItem[]): void {
    event.preventDefault();
    dismiss();

    const doc = document;
    const menu = doc.createElement("div");
    menu.className = "context-menu";
    menu.setAttribute("role", "menu");
    menu.setAttribute("aria-label", "Actions");
    for (const entry of items) {
        menu.append(
            menuItem(doc, entry.icon, entry.label, () => {
                dismiss();
                entry.run();
            }),
        );
    }

    const itemsOf = (): HTMLElement[] => [
        ...menu.querySelectorAll<HTMLElement>('[role="menuitem"]'),
    ];
    const onPointerDown = (e: Event): void => {
        if (!menu.contains(e.target as Node)) dismiss();
    };
    const onKeyDown = (e: KeyboardEvent): void => {
        if (e.key === "Escape") {
            e.preventDefault();
            dismiss();
        } else if (e.key === "ArrowDown" || e.key === "ArrowUp") {
            e.preventDefault();
            const all = itemsOf();
            const index = all.indexOf(doc.activeElement as HTMLElement);
            const step = e.key === "ArrowDown" ? 1 : -1;
            all[(index + step + all.length) % all.length]?.focus();
        }
    };
    doc.addEventListener("pointerdown", onPointerDown, true);
    doc.addEventListener("keydown", onKeyDown, true);
    dismissActive = (): void => {
        menu.remove();
        doc.removeEventListener("pointerdown", onPointerDown, true);
        doc.removeEventListener("keydown", onKeyDown, true);
    };

    menu.style.left = `${event.clientX}px`;
    menu.style.top = `${event.clientY}px`;
    doc.body.append(menu);
    // Keep the menu within the viewport when opened near the right or bottom edge.
    const view = doc.defaultView;
    if (view) {
        const overflowX = event.clientX + menu.offsetWidth - view.innerWidth + 8;
        const overflowY = event.clientY + menu.offsetHeight - view.innerHeight + 8;
        if (overflowX > 0) menu.style.left = `${event.clientX - overflowX}px`;
        if (overflowY > 0) menu.style.top = `${event.clientY - overflowY}px`;
    }
    itemsOf()[0]?.focus();
}

// A context-menu entry: an icon and a label, VS Code style.
function menuItem(
    doc: Document,
    iconName: string,
    label: string,
    onClick: () => void,
): HTMLButtonElement {
    const button = doc.createElement("button");
    button.type = "button";
    button.className = "context-menu-item";
    button.setAttribute("role", "menuitem");
    const text = doc.createElement("span");
    text.className = "context-menu-label";
    text.textContent = label;
    button.append(codicon(iconName, "context-menu-icon"), text);
    button.addEventListener("click", onClick);
    return button;
}
