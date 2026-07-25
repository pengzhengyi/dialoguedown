// Factor each zoom-in/out button press multiplies (or divides) the scale by.
export const ZOOM_STEP = 1.3;

// A revert glyph (anticlockwise circle arrow) — "reset to the default view".
const REVERT_GLYPH = "\u21BA";

export interface ZoomHandlers {
    onZoomIn(): void;
    onZoomOut(): void;
    /** Set the zoom to an explicit percentage the reader typed (e.g. 150 -> 1.5x). */
    onSetZoom(percent: number): void;
    /** Reset this graph to the default framing and drop its remembered position. */
    onRevert(): void;
}

export interface ZoomControls {
    element: HTMLElement;
    /** Reflect the current scale in the input (0.85 -> "85"), unless it is being edited. */
    setRatio(scale: number): void;
}

/** The "− [editable %] + ↺" zoom widget: step, type a percentage, or revert. */
export function createZoomControls(handlers: ZoomHandlers): ZoomControls {
    const container = document.createElement("div");
    container.className = "zoom-controls";

    container.append(controlButton("\u2212", "Zoom out", handlers.onZoomOut));
    const input = zoomInput(handlers.onSetZoom);
    container.append(input.field);
    container.append(controlButton("+", "Zoom in", handlers.onZoomIn));
    container.append(controlButton(REVERT_GLYPH, "Revert to default view", handlers.onRevert));

    return {
        element: container,
        setRatio(scale) {
            // Do not overwrite the value while the reader is typing into it.
            if (document.activeElement !== input.element) {
                input.element.value = String(Math.round(scale * 100));
            }
        },
    };
}

/** A number field the reader types a zoom percentage into, with a "%" suffix. */
function zoomInput(onSetZoom: (percent: number) => void): {
    field: HTMLElement;
    element: HTMLInputElement;
} {
    const field = document.createElement("div");
    field.className = "zoom-field";

    const input = document.createElement("input");
    input.type = "number";
    input.className = "zoom-input zoom-ratio";
    input.min = "10";
    input.max = "300";
    input.step = "10";
    input.value = "100";
    input.title = "Zoom percent";
    input.setAttribute("aria-label", "Zoom percent");

    const commit = (): void => {
        const percent = Number(input.value);
        if (Number.isFinite(percent) && percent > 0) onSetZoom(percent);
    };
    input.addEventListener("change", commit);
    input.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            commit();
            input.blur();
        }
    });

    const suffix = document.createElement("span");
    suffix.className = "zoom-suffix";
    suffix.textContent = "%";
    suffix.setAttribute("aria-hidden", "true");

    field.append(input, suffix);
    return { field, element: input };
}

function controlButton(text: string, ariaLabel: string, onClick: () => void): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = text;
    button.title = ariaLabel;
    button.setAttribute("aria-label", ariaLabel);
    button.addEventListener("click", onClick);
    return button;
}
