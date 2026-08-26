import type { TagView } from "./model";

/**
 * One tag, drawn the same capsule wherever the report shows it — the Config tab, the Semantic
 * Model, and the Playbook.
 *
 * Color carries two things at once, deliberately kept apart. The capsule itself keeps the
 * palette's canonical **tag** hue, so a reader who has learned that pink means "tag" in the graph
 * legend reads it the same way in a table; reserved names DialogueDown owns wear a distinct
 * violet, because for them the kind *is* the identity. The tag's own identity moves to a small
 * leading dot, whose hue is derived from the tag's name — so `#wise` wears the same dot in every
 * table and every tab, and `role=guide` shares its dot with `role=merchant`.
 */

/**
 * Hues for the identity dot. Deliberately a separate, small set from the semantic
 * {@link CATEGORY_COLORS} palette: these say "which tag", never "what kind of thing", and are
 * picked to stay apart from each other and legible on both themes.
 */
const IDENTITY_HUES: readonly string[] = [
    "#0ea5e9",
    "#8b5cf6",
    "#f97316",
    "#10b981",
    "#e11d48",
    "#eab308",
    "#06b6d4",
    "#d946ef",
];

/**
 * The identity hue for a tag name — stable across reloads, tables, and tabs, because it is
 * derived from the name rather than from the order tags happen to appear in.
 */
export function tagHue(name: string): string {
    let hash = 0;
    for (let i = 0; i < name.length; i += 1) {
        hash = (hash * 31 + name.charCodeAt(i)) >>> 0;
    }
    return IDENTITY_HUES[hash % IDENTITY_HUES.length];
}

/** The tag as a script writes it: `#name`, `#name=value`, or `##name` when reserved. */
export function tagLabel(tag: TagView): string {
    const prefix = tag.reserved ? "##" : "#";
    return tag.value == null ? `${prefix}${tag.name}` : `${prefix}${tag.name}=${tag.value}`;
}

/**
 * One capsule. It carries `data-copy`, which is what the Config tab's delegated copy handler
 * looks for, so a reader can lift the tag straight into a script exactly as it is written.
 */
export function renderTag(tag: TagView): HTMLElement {
    const chip = document.createElement("span");
    chip.className = `dd-tag ${tag.reserved ? "dd-tag-reserved" : "dd-tag-custom"}`;
    chip.dataset.copy = tagLabel(tag);
    chip.title = tagLabel(tag);

    // A reserved name is one of a closed set, so its violet already identifies it; only a
    // writer's own tag needs the dot to tell it from the next one.
    if (!tag.reserved) {
        const dot = document.createElement("span");
        dot.className = "dd-tag-dot";
        dot.style.setProperty("--dd-tag-hue", tagHue(tag.name));
        chip.appendChild(dot);
    }

    const text = document.createElement("span");
    text.className = "dd-tag-text";
    text.textContent = tagLabel(tag);
    chip.appendChild(text);
    return chip;
}

/** A cell's worth of tags, or an empty element when the speaker carries none. */
export function renderTags(tags: readonly TagView[]): HTMLElement {
    const wrap = document.createElement("div");
    wrap.className = "dd-tags";
    tags.forEach((tag) => wrap.appendChild(renderTag(tag)));
    return wrap;
}
