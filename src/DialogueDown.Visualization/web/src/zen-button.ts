import { codicon } from "./codicon";

/**
 * A Zen-mode toggle button with the concentric-circles codicon (`target`) that VS Code shows
 * beside its own **Zen Mode** command.
 *
 * {@link ./fullscreen!initFullscreen} sets the pressed state from the root class rather than per
 * button, so a button built while Zen is already on still reads correctly.
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
