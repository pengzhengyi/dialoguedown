import { codicon } from "./codicon";

/**
 * A Zen-mode toggle button carrying the same concentric-circles codicon VS Code shows beside
 * its own **Zen Mode** command (`target`). Using VS Code's glyph makes the mode recognizable
 * to readers who already know it, and it reads as a different idea from the maximize
 * button's outward arrows sitting next to it rather than a second flavour of full screen.
 *
 * Deliberately not `layout-centered`: that is the glyph VS Code uses for its separate
 * **Centered Layout** command, so borrowing it here would name a different feature.
 *
 * The pressed state is reflected by {@link ./fullscreen!initFullscreen} from the root class,
 * not per-button, so a button built while Zen is already on still reads correctly.
 */
export function createZenButton(onToggle: () => void): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "zen-button";
    button.title = "Zen mode (z)";
    button.setAttribute("aria-label", "Zen mode");
    button.setAttribute("aria-pressed", "false");
    button.appendChild(codicon("target", "zen-icon"));
    button.addEventListener("click", onToggle);
    return button;
}
