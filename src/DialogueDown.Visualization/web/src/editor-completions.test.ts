import { describe, it, expect } from "vitest";
import { EditorState } from "@codemirror/state";
import { EditorView } from "@codemirror/view";
import { runScopeHandlers } from "@codemirror/view";
import {
    CompletionContext,
    completionStatus,
    startCompletion,
    type Completion,
    type CompletionResult,
    type CompletionSource,
} from "@codemirror/autocomplete";
import type { DialogueSymbolProvider, DialogueSymbols } from "./model";
import {
    jumpIndicatorCompletions,
    jumpSlugCompletions,
    speakerIdCompletions,
    tagCompletions,
    speakerCompletions,
    dialogueAutocompletion,
} from "./editor-completions";

/** A symbol provider carrying just the parts a test needs; the rest default to empty. */
function provide(partial: Partial<DialogueSymbols>): DialogueSymbolProvider {
    return () => ({ jumpTargets: [], speakers: [], speakerIds: [], tags: [], ...partial });
}

/** Build a completion context with the cursor at `|` in `docWithCursor`. */
function contextAtCursor(docWithCursor: string, explicit = false): CompletionContext {
    const pos = docWithCursor.indexOf("|");
    const doc = docWithCursor.slice(0, pos) + docWithCursor.slice(pos + 1);
    return new CompletionContext(EditorState.create({ doc }), pos, explicit);
}

/** The labels a source offers at the cursor, or `null` when it does not fire. */
function labelsAt(source: CompletionSource, docWithCursor: string): string[] | null {
    const result = resultAt(source, docWithCursor);
    return result ? result.options.map((o) => o.label) : null;
}

/** The (synchronous) completion result a source returns at the cursor, or `null`. */
function resultAt(source: CompletionSource, docWithCursor: string): CompletionResult | null {
    return source(contextAtCursor(docWithCursor)) as CompletionResult | null;
}

/**
 * Apply `option` over the range `result.from`…cursor in `docWithCursor`, returning the
 * resulting document text and whatever range the option leaves selected (a snippet field).
 */
function applyAt(
    result: CompletionResult,
    option: Completion,
    docWithCursor: string,
): { text: string; selected: string } {
    const pos = docWithCursor.indexOf("|");
    const doc = docWithCursor.slice(0, pos) + docWithCursor.slice(pos + 1);
    const parent = document.createElement("div");
    document.body.appendChild(parent);
    const view = new EditorView({
        parent,
        state: EditorState.create({ doc, selection: { anchor: pos } }),
    });
    const apply = option.apply as (
        view: EditorView,
        completion: Completion,
        from: number,
        to: number,
    ) => void;
    apply(view, option, result.from, pos);
    const { from, to } = view.state.selection.main;
    const applied = { text: view.state.doc.toString(), selected: view.state.sliceDoc(from, to) };
    view.destroy();
    parent.remove();
    return applied;
}

describe("jumpIndicatorCompletions", () => {
    const source = jumpIndicatorCompletions(
        provide({
            jumpTargets: [
                { slug: "the-market", heading: "The Market" },
                { slug: "the-old-mill", heading: "The Old Mill" },
            ],
        }),
    );

    it("offers every scene, labelled by its heading, after the => indicator", () => {
        expect(labelsAt(source, `Alice: hi\n=> |`)).toEqual(["The Market", "The Old Mill"]);
    });

    it("fires immediately after => with no trailing space", () => {
        expect(labelsAt(source, `=>|`)).toEqual(["The Market", "The Old Mill"]);
    });

    it("filters scenes by the partial label via the completion's from", () => {
        const doc = `=> The O|`;
        const result = resultAt(source, doc)!;
        // `from` points just past `=> `, so the whole partial label is the filter prefix.
        const state = EditorState.create({ doc: doc.replace("|", "") });
        expect(state.doc.sliceString(result.from, doc.indexOf("|"))).toBe("The O");
    });

    it("shows the slug as detail and previews the full anchor as info", () => {
        const result = resultAt(source, `=> |`)!;
        expect(result.options[0]).toMatchObject({
            label: "The Market",
            detail: "#the-market",
            info: "[The Market](#the-market)",
        });
    });

    it("types each option as dd-jump (selects the arrow icon)", () => {
        const result = resultAt(source, `=> |`)!;
        expect(result.options.every((o) => o.type === "dd-jump")).toBe(true);
    });

    it("enriches an accepted scene into the whole [Heading](#slug) jump target", () => {
        const result = resultAt(source, `=> |`)!;
        const market = result.options.find((o) => o.label === "The Market")!;
        expect(applyAt(result, market, `=> |`).text).toBe(`=> [The Market](#the-market)`);
    });

    it("leaves the heading as an editable field, its default text selected", () => {
        const result = resultAt(source, `=> |`)!;
        const market = result.options.find((o) => o.label === "The Market")!;
        // The snippet selects the heading field, so a writer can immediately retype the label.
        expect(applyAt(result, market, `=> |`).selected).toBe("The Market");
    });

    it("normalizes the separating space so => [..] never becomes =>[..]", () => {
        const result = resultAt(source, `=>|`)!;
        const market = result.options.find((o) => o.label === "The Market")!;
        expect(applyAt(result, market, `=>|`).text).toBe(`=> [The Market](#the-market)`);
    });

    it("replaces a partial label rather than appending to it", () => {
        const result = resultAt(source, `=> The O|`)!;
        const mill = result.options.find((o) => o.label === "The Old Mill")!;
        expect(applyAt(result, mill, `=> The O|`).text).toBe(`=> [The Old Mill](#the-old-mill)`);
    });

    it("does not fire once the link brackets are typed (left to the slug source)", () => {
        expect(labelsAt(source, `=> [x](#|)`)).toBeNull();
    });

    it("does not fire on a line without the => indicator", () => {
        expect(labelsAt(source, `Alice: plain text |`)).toBeNull();
    });

    it("offers nothing when the payload has no scenes", () => {
        const empty = jumpIndicatorCompletions(provide({}));
        expect(labelsAt(empty, `=> |`)).toBeNull();
    });

    it("offers the #END sentinel like any other scene", () => {
        const withEnd = jumpIndicatorCompletions(
            provide({ jumpTargets: [{ slug: "END", heading: "End the run" }] }),
        );
        const result = resultAt(withEnd, `=> |`)!;
        expect(applyAt(result, result.options[0], `=> |`).text).toBe(`=> [End the run](#END)`);
    });
});

describe("jumpSlugCompletions", () => {
    const source = jumpSlugCompletions(
        provide({
            jumpTargets: [
                { slug: "the-market", heading: "The Market" },
                { slug: "the-old-mill", heading: "The Old Mill" },
            ],
        }),
    );

    it("offers the payload's heading slugs inside a jump destination", () => {
        const doc = `Alice: Go [east](#|)`;
        expect(labelsAt(source, doc)).toEqual(["the-market", "the-old-mill"]);
    });

    it("filters against the partial slug via the completion's from", () => {
        const doc = `Alice: Go [east](#the-m|)`;
        const result = resultAt(source, doc)!;
        // `from` points just after `#`, so the whole partial slug is the filter prefix.
        const state = EditorState.create({ doc: doc.replace("|", "") });
        expect(state.doc.sliceString(result.from, doc.indexOf("|"))).toBe("the-m");
    });

    it("shows the heading text as the option detail", () => {
        const result = resultAt(source, `Alice: [x](#|)`)!;
        expect(result.options[0]).toMatchObject({ label: "the-market", detail: "The Market" });
    });

    it("types each option as dd-jump (selects the arrow icon)", () => {
        const result = resultAt(source, `Alice: [x](#|)`)!;
        expect(result.options.every((o) => o.type === "dd-jump")).toBe(true);
    });

    it("does not fire outside a jump destination", () => {
        expect(labelsAt(source, `Alice: plain text |`)).toBeNull();
    });

    it("offers nothing when the payload has no jump targets", () => {
        const empty = jumpSlugCompletions(provide({}));
        expect(labelsAt(empty, `Alice: Go [east](#|)`)).toBeNull();
    });
});

describe("speakerIdCompletions", () => {
    const source = speakerIdCompletions(provide({ speakerIds: ["guide", "merchant"] }));

    it("offers the payload's ids after an @", () => {
        expect(labelsAt(source, `@|`)).toEqual(["guide", "merchant"]);
    });

    it("does not fire without an @ before the cursor", () => {
        expect(labelsAt(source, `Alice: plain|`)).toBeNull();
    });

    it("excludes the fully-typed id from its own suggestions", () => {
        // Typing a whole known id back is noise; the exact match is dropped.
        expect(labelsAt(source, `@guide|`)).toEqual(["merchant"]);
    });

    it("types each option as dd-speaker-id (selects the @ icon)", () => {
        const result = resultAt(source, `@|`)!;
        expect(result.options.every((o) => o.type === "dd-speaker-id")).toBe(true);
    });
});

describe("tagCompletions", () => {
    const source = tagCompletions(provide({ tags: ["wise", "happy"] }));

    it("offers the payload's tags after a mid-line #", () => {
        expect(labelsAt(source, `Bob #|`)).toEqual(["wise", "happy"]);
    });

    it("does not fire on a line-start # (a Markdown heading)", () => {
        expect(labelsAt(source, `#|`)).toBeNull();
    });

    it("does not fire inside a jump destination", () => {
        expect(labelsAt(source, `Alice: [x](#|)`)).toBeNull();
    });

    it("types each option as dd-tag (selects the # icon)", () => {
        const result = resultAt(source, `Bob #|`)!;
        expect(result.options.every((o) => o.type === "dd-tag")).toBe(true);
    });
});

describe("speakerCompletions", () => {
    const source = speakerCompletions(provide({ speakers: ["Alice", "Guide"] }));

    it("offers the payload's speakers at the start of a line", () => {
        // Returns every known speaker; CodeMirror filters by the typed prefix.
        expect(labelsAt(source, `A|`)).toEqual(["Alice", "Guide"]);
    });

    it("does not fire mid-line, after the speaker name", () => {
        expect(labelsAt(source, `Alice: some text |`)).toBeNull();
    });

    it("offers nothing when the payload has no speakers", () => {
        const empty = speakerCompletions(provide({}));
        expect(labelsAt(empty, `A|`)).toBeNull();
    });

    it("types each option as dd-speaker (selects the person icon)", () => {
        const result = resultAt(source, `A|`)!;
        expect(result.options.every((o) => o.type === "dd-speaker")).toBe(true);
    });
});

describe("dialogueAutocompletion keymap", () => {
    /** Mount an editable editor with the dialogue completions, cursor at the end of `doc`. */
    function mount(doc: string): EditorView {
        const parent = document.createElement("div");
        document.body.appendChild(parent);
        return new EditorView({
            parent,
            state: EditorState.create({
                doc,
                selection: { anchor: doc.length },
                extensions: [dialogueAutocompletion(provide({ speakers: ["Alice"] }))],
            }),
        });
    }

    /**
     * Open the completion tooltip and wait until Tab/Enter would actually accept it.
     * Two waits are needed: the completion query is async (poll until `active`), and
     * CodeMirror enforces a 75 ms `interactionDelay` after a tooltip opens — an
     * anti-misclick guard that blocks accepting a *just*-opened completion (it applies
     * to Enter too). A human always clears it; the extra wait mirrors that.
     */
    async function openCompletion(view: EditorView, timeout = 1000): Promise<void> {
        startCompletion(view);
        const start = Date.now();
        while (completionStatus(view.state) !== "active") {
            if (Date.now() - start > timeout) throw new Error("completion never activated");
            await new Promise((resolve) => setTimeout(resolve, 10));
        }
        await new Promise((resolve) => setTimeout(resolve, 120)); // clear interactionDelay
    }

    /** Send Tab through the editor's keymap; returns whether a binding consumed it. */
    function pressTab(view: EditorView): boolean {
        const event = new KeyboardEvent("keydown", { key: "Tab" });
        return runScopeHandlers(view, event, "editor");
    }

    it("accepts the active completion, like Enter", async () => {
        const view = mount(`Alice: Hi.

A`);
        await openCompletion(view);
        const consumed = pressTab(view);
        expect(view.state.doc.toString()).toBe(`Alice: Hi.

Alice`);
        expect(consumed).toBe(true);
        view.destroy();
    });

    it("leaves Tab alone when no completion is open, so focus can exit the editor", () => {
        const view = mount(`Alice: Hi.

A`);
        // No startCompletion: with the tooltip closed, Tab must fall through to its
        // default (moving focus), never getting swallowed by the editor.
        const consumed = pressTab(view);
        expect(view.state.doc.toString()).toBe(`Alice: Hi.

A`);
        expect(consumed).toBe(false);
        view.destroy();
    });
});
