import type { DisplayNode, Span } from "./model";
import { colorOf } from "./palette";
import { escapeHtml, renderNodePreview } from "./text";
import { createJumpButton, type JumpButton } from "./jump-button";

export interface DetailPanelOptions {
    /**
     * Jump to the selected node's source in the Source tab — selecting its span, or placing the
     * cursor at a synthetic node's zero-width position. Absent when there is no Source tab to jump
     * to (a single-graph render), which also hides the inspector's jump affordance.
     */
    jumpToSource?: (span: Span) => void;
}

export interface DetailPanel {
    show(node: DisplayNode, preview?: NodePreviewOptions): void;
    clear(): void;
}

export interface NodePreviewOptions {
    /** The active stage has recognized Dialogue jump syntax. */
    recognizeJumps?: boolean;
}

/** The body HTML shown when no node is selected. */
export const NODE_DETAIL_PLACEHOLDER =
    "<p>Click any node to see the source it was produced from, and a rendered preview. " +
    "Use <strong>Jump to source</strong> to edit it in the Source tab.</p>";

/** The title HTML for a node's detail: a category color dot beside the node's label. */
export function nodeDetailTitle(node: DisplayNode): string {
    return categoryDot(node.category) + escapeHtml(node.label);
}

/** The body HTML for a node's detail: its attributes, then its source and a rendered preview. */
export function nodeDetailBody(node: DisplayNode, preview: NodePreviewOptions = {}): string {
    return attributesTable(node.attributes) + sourceSection(node, preview);
}

/**
 * The graph tabs' node inspector: a selected node's category, attributes, the source it was
 * produced from, and a rendered preview.
 *
 * It is deliberately **read-only**. Editing lives in one place — the Source tab — and the
 * **Jump to source** action beside the title takes the reader there with the node's span already
 * selected. This panel and the Semantic tab's node-details panel therefore render identically
 * through {@link nodeDetailTitle} and {@link nodeDetailBody}; only where they mount differs.
 */
export function createDetailPanel(options: DetailPanelOptions = {}): DetailPanel {
    const titleEl = document.getElementById("detail-title")!;
    const bodyEl = document.getElementById("detail-body")!;

    // A "Jump to source" affordance (served or export, whenever there is a Source tab): an icon
    // button placed beside the node title that takes the reader to the node's span in the Source
    // editor. Shown only for a node that maps to a position — which includes a synthetic node
    // (a zero-width caret). Reused, not rebuilt, across selections.
    const jump: JumpButton | null = options.jumpToSource
        ? createJumpButton(options.jumpToSource)
        : null;

    // Render the title, then re-append the reused jump button (setting the title HTML drops any
    // prior child) and reflect the node on it.
    function renderTitle(html: string, node: DisplayNode | null): void {
        titleEl.innerHTML = html;
        if (jump) {
            titleEl.appendChild(jump.element);
            jump.update(node);
        }
    }

    bodyEl.innerHTML = NODE_DETAIL_PLACEHOLDER;

    return {
        show(node, preview = {}) {
            renderTitle(nodeDetailTitle(node), node);
            bodyEl.innerHTML = nodeDetailBody(node, preview);
        },
        clear() {
            renderTitle(escapeHtml("Node details"), null);
            bodyEl.innerHTML = NODE_DETAIL_PLACEHOLDER;
        },
    };
}

// A color dot ties the node to its legend color without repeating a category
// name (the node's own label already appears beside it).
function categoryDot(category: string | undefined): string {
    if (!category) return "";
    return `<span class="dot" style="background:${colorOf(category)}"></span>`;
}

function attributesTable(attributes: DisplayNode["attributes"]): string {
    if (!attributes.length) return "";
    const rows = attributes
        .map(
            (attr) =>
                `<tr><th scope="row">${escapeHtml(attr.name)}</th><td>${escapeHtml(attr.value)}</td></tr>`,
        )
        .join("");
    return `<table><tbody>${rows}</tbody></table>`;
}

function sourceSection(node: DisplayNode, preview: NodePreviewOptions = {}): string {
    const { source } = node;
    // A node with no source is synthetic — a stage inserted it (a filled default
    // speaker), so it maps to no text. Say so, instead of an empty Source block.
    if (typeof source !== "string") {
        return `<p class="inserted-note">Inserted by the compiler — no source of its own.</p>`;
    }
    return (
        `<h4>Source</h4><pre><code>${escapeHtml(source)}</code></pre>` +
        `<h4>Preview</h4><div class="preview">${renderNodePreview(
            source,
            node.label,
            preview.recognizeJumps ?? false,
        )}</div>`
    );
}
