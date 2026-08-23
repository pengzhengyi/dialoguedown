import { hoverTooltip } from "@codemirror/view";
import type { EditorView, Tooltip } from "@codemirror/view";
import type { EditorState } from "@codemirror/state";
import schema from "../../../../schema/playbook-0.schema.json";
import { escapeHtml } from "./text";

/**
 * Schema-driven hover for the playbook editor: hovering a property shows what the format says
 * that property means, the way an editor does for a `$schema`-linked JSON file.
 *
 * The descriptions come from the published playbook schema itself, imported here rather than
 * copied, so a schema edit reaches the report with nothing to keep in sync. It is bundled
 * instead of sent with each report because it describes the *format*, not this playbook — a
 * per-report copy would be re-sent on every save to say the same thing.
 *
 * A path is resolved *through* the schema on demand rather than flattened into a lookup table
 * up front. The playbook format is recursive — a fragment holds fragments — so a table of every
 * path would not terminate; following `$ref`s only as deep as the reader hovers does.
 */

/** How far a `$ref` chain is followed before the schema is assumed to be cyclic. */
const MAX_HOPS = 16;

type SchemaNode = Record<string, unknown>;

const definitions = (schema as SchemaNode).$defs as Record<string, SchemaNode> | undefined;

/** Follows `$ref`s into `$defs`, so a property described only by its definition still resolves. */
function dereference(node: unknown): SchemaNode | null {
    let current = node;
    for (let hop = 0; hop < MAX_HOPS; hop++) {
        if (typeof current !== "object" || current === null) return null;
        const ref = (current as SchemaNode).$ref;
        if (typeof ref !== "string") return current as SchemaNode;
        current = definitions?.[ref.split("/").pop() ?? ""];
    }
    return null;
}

/** The branches of a `oneOf`/`anyOf`/`allOf`, which the format uses to model tagged variants. */
function branchesOf(node: SchemaNode): unknown[] {
    return ["oneOf", "anyOf", "allOf"].flatMap((key) => (node[key] as unknown[]) ?? []);
}

/**
 * Every schema node reachable from these without consuming a path segment, most specific first.
 *
 * The format models its variants as a `oneOf` tagged by `kind`, and the document says which one
 * it is. Given that tag, the matching branch is offered ahead of the shape that holds it — the
 * difference between a hover that says "one piece of what a line says" and one that says "plain
 * words, as written". Without a usable tag every branch is offered, so an untagged union still
 * resolves.
 */
function expand(nodes: readonly unknown[], kind?: string): SchemaNode[] {
    const found: SchemaNode[] = [];
    for (const node of nodes) collect(node, kind, found);
    return found;
}

function collect(node: unknown, kind: string | undefined, found: SchemaNode[]): void {
    const resolved = dereference(node);
    if (resolved == null || found.includes(resolved) || found.length > MAX_HOPS * 4) return;
    const branches = branchesOf(resolved);
    const matched = kind == null ? [] : branches.filter((branch) => tagOf(branch) === kind);
    for (const branch of matched) collect(branch, undefined, found);
    found.push(resolved);
    if (matched.length === 0) for (const branch of branches) collect(branch, undefined, found);
}

/** The `const` a branch pins its `kind` to, when it is a tagged variant. */
function tagOf(branch: unknown): string | undefined {
    const properties = dereference(branch)?.properties as Record<string, SchemaNode> | undefined;
    const tag = properties?.kind?.const;
    return typeof tag === "string" ? tag : undefined;
}

/**
 * Descends one path segment. An array element is written `*`; anything else is a property name,
 * which falls back to `additionalProperties` so a map's own keys (`anchors/the-tavern`) resolve.
 */
function step(node: SchemaNode, segment: string): unknown[] {
    if (segment === "*") return node.items == null ? [] : [node.items];
    const properties = node.properties as Record<string, unknown> | undefined;
    const named = properties?.[segment];
    if (named != null) return [named];
    const rest = node.additionalProperties;
    return typeof rest === "object" && rest !== null ? [rest] : [];
}

/**
 * What the schema says about a path, or undefined when it describes nothing there.
 *
 * A tagged variant is searched across all of its branches and the first description wins: the
 * branches of a `oneOf` describe the same position, so any of them is a true answer, and
 * choosing between them would mean re-deriving the document's own discriminator.
 */
export function describeSchemaPath(
    path: string,
    kinds: readonly (string | undefined)[] = [],
): Described | null {
    const segments = path.split("/").filter((segment) => segment.length > 0);
    let frontier: unknown[] = [schema];
    // The deepest description reached so far. A leaf the format leaves undocumented — `kind` and
    // `text` inside a variant, which the schema documents as a whole — then reports the shape it
    // belongs to, labelled with that shape's path so the answer is never mistaken for the leaf's.
    // Only a path actually reached is recorded, so a path outside the format describes nothing.
    let best: Described | null = null;
    for (const [index, segment] of segments.entries()) {
        const next = expand(frontier, kinds[index]).flatMap((node) => step(node, segment));
        if (next.length === 0) return best;
        frontier = next;
        const described = describedIn(expand(frontier, kinds[index + 1]));
        if (described != null) {
            best = { path: segments.slice(0, index + 1).join("/"), ...described };
        }
    }
    return best;
}

/** A description the schema gives, and the path it actually describes. */
export interface Described {
    /** The path the description belongs to — the hovered one, or the shape enclosing it. */
    readonly path: string;
    /** What the format says about that path. */
    readonly description: string;
}

function describedIn(nodes: readonly SchemaNode[]): { description: string } | undefined {
    const found = nodes.find((node) => typeof node.description === "string");
    return found == null ? undefined : { description: found.description as string };
}

/** A property line, capturing its indentation and name: `  "requires": [`. */
const PROPERTY = /^(\s*)"((?:[^"\\]|\\.)*)"\s*:/;
/** The discriminator line the format tags a variant with: `  "kind": "line",`. */
const KIND = /^\s*"kind"\s*:\s*"((?:[^"\\]|\\.)*)"/;
/** Any line's indentation, which the writer emits as two spaces per level. */
const INDENT = /^\s*/;

function depthOf(line: string): number {
    return (INDENT.exec(line)?.[0].length ?? 0) / 2;
}

/**
 * The schema path of a line in the rendered playbook.
 *
 * The document is written by `JsonSerializer` with `WriteIndented`, whose shape is exactly
 * regular: two spaces per level, one property per line, and no literal newline inside a string.
 * So a line's path is read by walking up to each shallower ancestor — a property line
 * contributes its name, and a bare `{` or `[` contributes the `*` of an array element.
 */
export function schemaPathAt(state: EditorState, lineNumber: number): Located {
    const line = state.doc.line(lineNumber);
    const own = PROPERTY.exec(line.text);
    const segments = [own ? own[2] : "*"];
    // One tag per level, read from the document, so a variant resolves to the branch it really is.
    const kinds: (string | undefined)[] = [kindBeside(state, lineNumber, depthOf(line.text))];
    let wanted = depthOf(line.text) - 1;
    for (let n = lineNumber - 1; n >= 1 && wanted >= 1; n--) {
        const text = state.doc.line(n).text;
        if (depthOf(text) !== wanted) continue;
        const property = PROPERTY.exec(text);
        segments.unshift(property ? property[2] : "*");
        kinds.unshift(kindBeside(state, n, wanted));
        wanted--;
    }
    return { path: segments.join("/"), kinds };
}

/** A path in the document, and the `kind` tag the document gives at each level of it. */
export interface Located {
    /** The schema path, with `*` for an array element. */
    readonly path: string;
    /** The `kind` at each level, aligned to the path's segments; `undefined` where untagged. */
    readonly kinds: readonly (string | undefined)[];
}

/**
 * The `kind` the document gives the object this line belongs to — the discriminator the format
 * tags its variants with. It is a *sibling* of the line, so the search runs over the enclosing
 * object's own properties, in both directions, and stops at its braces.
 */
function kindBeside(state: EditorState, lineNumber: number, depth: number): string | undefined {
    for (const stride of [-1, 1]) {
        for (let n = lineNumber; n >= 1 && n <= state.doc.lines; n += stride) {
            const text = state.doc.line(n).text;
            if (depthOf(text) < depth) break;
            if (depthOf(text) !== depth) continue;
            const tag = KIND.exec(text);
            if (tag) return tag[1];
        }
    }
    return undefined;
}

/** The `"name"` span of a property line, or the value span of a bare array element. */
function tokenRange(text: string, from: number): { start: number; end: number } {
    const property = PROPERTY.exec(text);
    if (property) {
        const start = from + property[1].length;
        return { start, end: start + property[2].length + 2 };
    }
    const indent = INDENT.exec(text)?.[0].length ?? 0;
    return { start: from + indent, end: from + text.trimEnd().length };
}

/**
 * The hover extension. A tooltip appears only over the property name (or an array element's
 * value) and only when the schema actually describes it, so hovering punctuation or a blank
 * stretch stays quiet.
 */
export function schemaHover() {
    return hoverTooltip((view: EditorView, position: number): Tooltip | null => {
        const line = view.state.doc.lineAt(position);
        const { start, end } = tokenRange(line.text, line.from);
        if (position < start || position > end) return null;
        const located = schemaPathAt(view.state, line.number);
        const described = describeSchemaPath(located.path, located.kinds);
        if (described == null) return null;
        return {
            pos: start,
            end,
            above: true,
            create: () => ({ dom: tooltipDom(described) }),
        };
    });
}

function tooltipDom(described: Described): HTMLElement {
    const dom = document.createElement("div");
    dom.className = "playbook-hover";
    dom.innerHTML =
        `<code class="playbook-hover-path">${escapeHtml(described.path || "(document)")}</code>` +
        `<p class="playbook-hover-text">${escapeHtml(described.description)}</p>`;
    return dom;
}
