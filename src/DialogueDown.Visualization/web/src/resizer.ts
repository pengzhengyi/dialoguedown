/** @file Lets a reader drag the divider to resize the detail panel. */

/** Make the detail panel draggable to resize via the divider. */
export function initResizer(): void {
    const resizer = document.getElementById("resizer");
    const detail = document.getElementById("detail");
    const app = document.getElementById("app");
    if (!resizer || !detail) return;

    const minWidth = 280;
    const maxWidth = 760;
    let dragging = false;
    let rightEdge = window.innerWidth;

    resizer.addEventListener("mousedown", (event) => {
        // A collapsed inspector has nothing to resize — the divider is just its re-open
        // handle, so ignore drags (the toggle itself already swallows its own mousedown).
        if (detail.parentElement?.classList.contains("detail-collapsed")) return;
        // When the layout stacks (narrow screens), the divider is a horizontal collapse bar
        // only — the graph doesn't re-fit on resize, so a width drag makes no sense there.
        if (app && getComputedStyle(app).flexDirection === "column") return;
        dragging = true;
        // The detail panel's right edge is the reference for the width: with the content
        // area padded, that edge is inset from the window, so measure it rather than
        // assuming the viewport edge. (getBoundingClientRect is 0 under jsdom — fall back.)
        rightEdge = detail.getBoundingClientRect().right || window.innerWidth;
        document.body.style.userSelect = "none";
        event.preventDefault();
    });

    document.addEventListener("mousemove", (event) => {
        if (!dragging) return;
        const width = rightEdge - event.clientX;
        // Drive a CSS variable (not an inline flex-basis) so the stacked media query, which
        // reassigns `flex`, cleanly ignores the side-by-side width instead of inheriting it.
        detail.style.setProperty(
            "--detail-size",
            `${Math.max(minWidth, Math.min(maxWidth, width))}px`,
        );
    });

    document.addEventListener("mouseup", () => {
        dragging = false;
        document.body.style.userSelect = "";
    });
}
