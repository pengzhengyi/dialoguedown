import tippy, { type Instance } from "tippy.js";

/**
 * A short popover beside whatever a jump landed on, saying how the reader got there.
 *
 * A line inside the inspector is easy to miss: after a jump the reader is looking at the drawing,
 * not the panel beside it. The note therefore appears where they are already looking, and stays
 * until their next click rather than fading on a timer like a confirmation toast — it explains
 * something they did not ask for and may want to read twice.
 *
 * It anchors to the element's rectangle rather than to the element itself, so it cannot collide
 * with the hover tooltip the graph already delegates onto every node.
 */
let current: Instance | null = null;

export function showArrivalNote(anchor: Element, message: string): void {
    hideArrivalNote();

    const content = document.createElement("div");
    content.className = "dd-arrival-note";
    content.setAttribute("role", "status");
    content.textContent = message;

    current = tippy(document.body, {
        getReferenceClientRect: () => anchor.getBoundingClientRect(),
        content,
        allowHTML: true,
        interactive: true,
        placement: "bottom",
        maxWidth: 320,
        trigger: "manual",
        // The next click outside the note hides it.
        hideOnClick: true,
        onHidden: (instance) => {
            if (current === instance) current = null;
            instance.destroy();
        },
    });
    current.show();
}

export function hideArrivalNote(): void {
    current?.destroy();
    current = null;
}
