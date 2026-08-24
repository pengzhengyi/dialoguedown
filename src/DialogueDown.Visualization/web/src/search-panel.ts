import { EditorView, type Panel, type ViewUpdate } from "@codemirror/view";
import {
    search,
    SearchQuery,
    getSearchQuery,
    setSearchQuery,
    findNext,
    findPrevious,
    replaceNext,
    replaceAll,
    closeSearchPanel,
} from "@codemirror/search";

// Feather Icons (MIT), inline so the panel needs no icon dependency, matching the rest of the UI.
const CHEVRON_UP = svg('<polyline points="18 15 12 9 6 15" />');
const CHEVRON_DOWN = svg('<polyline points="6 9 12 15 18 9" />');
const CHEVRON_RIGHT = svg('<polyline points="9 18 15 12 9 6" />');
const CLOSE = svg('<line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />');

function svg(body: string): string {
    return (
        '<svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor"' +
        ` stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${body}</svg>`
    );
}

/**
 * The compact, VS Code-style find/replace panel. It owns only the presentation: every action
 * delegates to CodeMirror's own search commands (`findNext`, `replaceAll`, …) and the query
 * lives in the editor state via {@link setSearchQuery}, so the search behavior is CodeMirror's
 * — tested and unchanged. A single row holds the find field, the case/regex/word toggles, a
 * live match count, previous/next, and close; a chevron expands a second row for replace.
 */
class CompactSearchPanel implements Panel {
    readonly dom: HTMLElement;
    readonly top = true;

    private readonly findInput: HTMLInputElement;
    private readonly replaceInput: HTMLInputElement;
    private readonly count: HTMLElement;
    private readonly caseToggle: HTMLButtonElement;
    private readonly regexToggle: HTMLButtonElement;
    private readonly wordToggle: HTMLButtonElement;
    private readonly replaceParts: HTMLElement[];
    private readonly expandButton: HTMLButtonElement;
    private expanded = false;

    constructor(private readonly view: EditorView) {
        const query = getSearchQuery(view.state);

        this.dom = document.createElement("div");
        this.dom.className = "dd-search";
        // Keep the panel from stealing the editor's navigation keys; handle our own below.
        this.dom.addEventListener("keydown", (event) => this.onKeydown(event));

        this.expandButton = iconButton(CHEVRON_RIGHT, "Toggle Replace", "dd-search-expand");
        this.expandButton.setAttribute("aria-expanded", "false");
        this.expandButton.addEventListener("click", () => this.toggleReplace());

        this.findInput = field("Find", query.search);
        this.caseToggle = textToggle("Aa", "Match Case", query.caseSensitive);
        this.regexToggle = textToggle(".*", "Use Regular Expression", query.regexp);
        this.wordToggle = textToggle("ab|", "Match Whole Word", query.wholeWord);
        for (const toggle of [this.caseToggle, this.regexToggle, this.wordToggle]) {
            toggle.addEventListener("click", () => {
                const pressed = toggle.getAttribute("aria-pressed") === "true";
                toggle.setAttribute("aria-pressed", String(!pressed));
                this.commit();
                this.findInput.focus();
            });
        }
        this.findInput.addEventListener("input", () => this.commit());

        this.count = document.createElement("span");
        this.count.className = "dd-search-count";

        const prev = iconButton(CHEVRON_UP, "Previous Match (Shift+Enter)", "dd-search-nav");
        prev.addEventListener("click", () => this.run(findPrevious));
        const next = iconButton(CHEVRON_DOWN, "Next Match (Enter)", "dd-search-nav");
        next.addEventListener("click", () => this.run(findNext));
        const close = iconButton(CLOSE, "Close (Escape)", "dd-search-close");
        close.addEventListener("click", () => closeSearchPanel(this.view));

        const toggles = row(
            "dd-search-toggles",
            this.caseToggle,
            this.regexToggle,
            this.wordToggle,
        );
        const fieldWrap = row("dd-search-field", this.findInput, toggles);
        const findControls = row("dd-search-controls", this.count, prev, next, close);

        this.replaceInput = field("Replace", query.replace);
        this.replaceInput.classList.add("dd-search-replace-input");
        this.replaceInput.addEventListener("input", () => this.commit());
        const replace = actionButton("Replace", () => this.run(replaceNext));
        const replaceAllButton = actionButton("All", () => this.run(replaceAll));
        const replaceControls = row("dd-search-controls", replace, replaceAllButton);

        // A three-column grid — chevron | equal-width inputs | controls — keeps the find and
        // replace inputs the same width and left-aligns both control groups in the third column,
        // so the Replace/All buttons sit right after the input, under the count. Collapsing hides
        // the replace parts.
        this.replaceParts = [this.replaceInput, replaceControls];
        for (const part of this.replaceParts) part.hidden = true;

        this.dom.append(
            this.expandButton,
            fieldWrap,
            findControls,
            this.replaceInput,
            replaceControls,
        );
    }

    mount(): void {
        this.findInput.focus();
        this.findInput.select();
        this.updateCount();
    }

    update(update: ViewUpdate): void {
        // Keep the fields and the count in step when the query or the document changes elsewhere
        // (e.g. openSearchPanel pre-filled the query from the selection).
        const queryChanged = update.transactions.some((tr) =>
            tr.effects.some((effect) => effect.is(setSearchQuery)),
        );
        if (queryChanged) {
            this.syncFromState();
        }
        if (queryChanged || update.docChanged || update.selectionSet) {
            this.updateCount();
        }
    }

    /** Build a query from the current inputs and toggles and publish it to the editor state. */
    private commit(): void {
        this.view.dispatch({ effects: setSearchQuery.of(this.currentQuery()) });
        this.updateCount();
    }

    private currentQuery(): SearchQuery {
        return new SearchQuery({
            search: this.findInput.value,
            replace: this.replaceInput.value,
            caseSensitive: this.caseToggle.getAttribute("aria-pressed") === "true",
            regexp: this.regexToggle.getAttribute("aria-pressed") === "true",
            wholeWord: this.wordToggle.getAttribute("aria-pressed") === "true",
        });
    }

    // Run a search command, keeping focus in the find field so navigation stays keyboard-driven.
    private run(command: (view: EditorView) => boolean): void {
        command(this.view);
        this.updateCount();
    }

    private toggleReplace(): void {
        this.expanded = !this.expanded;
        for (const part of this.replaceParts) part.hidden = !this.expanded;
        this.expandButton.setAttribute("aria-expanded", String(this.expanded));
        this.expandButton.classList.toggle("expanded", this.expanded);
        (this.expanded ? this.replaceInput : this.findInput).focus();
    }

    private onKeydown(event: KeyboardEvent): void {
        if (event.key === "Escape") {
            event.preventDefault();
            closeSearchPanel(this.view);
        } else if (event.key === "Enter") {
            event.preventDefault();
            if (event.target === this.replaceInput) {
                this.run(event.shiftKey ? replaceAll : replaceNext);
            } else {
                this.run(event.shiftKey ? findPrevious : findNext);
            }
        }
    }

    private syncFromState(): void {
        const query = getSearchQuery(this.view.state);
        if (document.activeElement !== this.findInput) this.findInput.value = query.search;
        this.caseToggle.setAttribute("aria-pressed", String(query.caseSensitive));
        this.regexToggle.setAttribute("aria-pressed", String(query.regexp));
        this.wordToggle.setAttribute("aria-pressed", String(query.wholeWord));
    }

    private updateCount(): void {
        this.count.textContent = matchCountLabel(this.view, this.currentQuery());
    }
}

/** "3 of 12", "12 results", "No results", or empty when the field is empty. */
function matchCountLabel(view: EditorView, query: SearchQuery): string {
    if (query.search.length === 0) return "";
    if (!query.valid) return "No results";
    const cursor = query.getCursor(view.state);
    const selection = view.state.selection.main;
    let total = 0;
    let current = 0;
    for (let result = cursor.next(); !result.done; result = cursor.next()) {
        total += 1;
        if (result.value.from === selection.from && result.value.to === selection.to) {
            current = total;
        }
    }
    if (total === 0) return "No results";
    return current > 0 ? `${current} of ${total}` : `${total} result${total === 1 ? "" : "s"}`;
}

function field(placeholder: string, value: string): HTMLInputElement {
    const input = document.createElement("input");
    input.type = "text";
    input.className = "dd-search-input";
    input.placeholder = placeholder;
    input.setAttribute("aria-label", placeholder);
    input.value = value;
    return input;
}

function textToggle(label: string, title: string, pressed: boolean): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "dd-search-toggle";
    button.textContent = label;
    button.title = title;
    button.setAttribute("aria-label", title);
    button.setAttribute("aria-pressed", String(pressed));
    return button;
}

function iconButton(icon: string, title: string, className: string): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = className;
    button.innerHTML = icon;
    button.title = title;
    button.setAttribute("aria-label", title);
    return button;
}

function actionButton(label: string, onClick: () => void): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "dd-search-action";
    button.textContent = label;
    button.addEventListener("click", onClick);
    return button;
}

function row(className: string, ...children: HTMLElement[]): HTMLElement {
    const element = document.createElement("div");
    element.className = className;
    element.append(...children);
    return element;
}

/** The compact search extension both editors use in place of the default `search()` panel. */
export function compactSearch() {
    return search({ top: true, createPanel: (view) => new CompactSearchPanel(view) });
}
