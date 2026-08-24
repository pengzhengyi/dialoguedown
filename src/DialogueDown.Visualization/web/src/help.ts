/** Which tab's help to show: the Source tab, a stage graph tab, the Semantic tab, or — on the
 *  empty state — the Explorer sidebar. */
export type HelpContext = "source" | "graph" | "semantic" | "playbook" | "explorer";

const SOURCE_HELP = `
  <p><strong>Source &amp; preview.</strong> The left pane is the document as written;
     the right pane is a live Markdown preview. Scrolling either pane keeps matching
     Markdown blocks aligned in the other.</p>
  <p><strong>Syntax colors.</strong> The compiler distinguishes commands, value queries,
     conditions, static and dynamic random weights, control keywords, and the reserved
     <kbd>#END</kbd> target; ordinary code spans keep the Markdown code color.</p>
  <p><strong>Jump ligature.</strong> In the rendered preview, <kbd>=&gt;</kbd> before a
     link uses a bundled Fira Code ligature. The Source editor and saved script still keep
     the literal <kbd>=</kbd> and <kbd>&gt;</kbd> characters.</p>
  <p><strong>Preview links</strong> jump to their headings within the preview.</p>
  <p><strong>Reserved targets.</strong> The fixed row below the editor shows language-owned
     destinations that are not source headings. <strong>End</strong> (<kbd>#END</kbd>) uses an
     infinity marker; click the row to copy <kbd>[End](#END)</kbd>.</p>
  <p><strong>Jump to a stage.</strong> <strong>Right-click</strong> a selection (or press
     <kbd>Alt-J</kbd>) and choose <strong>Jump to&nbsp;▸</strong> a compiler stage to open that
     tab with the enclosing node revealed, centered, and brought up to a readable scale — the
     reverse of <strong>Jump to source</strong>. Works in View and Edit.</p>
  <p><strong>Drag the divider</strong> between the panes to re-proportion them, or use its
     <strong>hide handle</strong> to collapse the preview and give the editor the full
     width (click again to bring it back).</p>
  <p><strong>Editor.</strong> Find with <kbd>⌘/Ctrl-F</kbd> and fold a section from the
     gutter arrow. In Live Edit, format the selection with <kbd>⌘/Ctrl-B</kbd> (bold),
     <kbd>⌘/Ctrl-I</kbd> (italic) and <kbd>⌘/Ctrl-K</kbd> (link), or type <kbd>*</kbd>
     around a selection to emphasize it. <strong>Quote a block</strong> with
     <kbd>⌘/Ctrl-.</kbd> and <strong>unquote</strong> with <kbd>⌘/Ctrl-Shift-.</kbd>: quoting
     adds a <kbd>&gt;</kbd> to every line the selection touches and nests on an already-quoted
     line, and unquoting removes one level — handy for the blockquote-based block controls.
     <strong>Right-click</strong> the editor for the same surround actions as a menu — bold,
     italic, strikethrough, quote, and unquote. <kbd>Tab</kbd> indents at the start of a line
     (or a multi-line selection) and inserts spaces elsewhere, <kbd>Shift-Tab</kbd> outdents (in
     Live Edit); press <kbd>Esc</kbd> to move focus out of the editor. To learn more about how to
     use the editor, check out
     <a href="https://codemirror.net/" target="_blank" rel="noopener noreferrer">CodeMirror</a>.</p>
  <p><strong>Autocomplete</strong> (Live Edit): as you type, the editor suggests names
     from the document — a jump target after <kbd>](#</kbd>, a speaker id after
     <kbd>@</kbd>, a <kbd>#</kbd>tag, or a speaker at the start of a line. <kbd>Enter</kbd>
     or <kbd>Tab</kbd> accepts, <kbd>Esc</kbd> dismisses.</p>
  <p><strong>Save modes &amp; status</strong> (Live Edit): the <strong>Auto | Manual</strong>
     capsule sets how the active document saves — <strong>Auto</strong> writes 1s after you
     stop typing, <strong>Manual</strong> waits for you. Source starts Auto and Config starts
     Manual, and each choice is remembered. The status beside it reads
     <em>Unsaved / Saving… / Saved</em>, or <em>Conflict</em>, <em>Waiting for valid TOML</em>,
     or <em>Saved — invalid TOML</em> when relevant.</p>
  <p><strong>Save &amp; discard</strong> (Live Edit): <kbd>⌘/Ctrl-S</kbd> or the
     <strong>Save</strong> button writes immediately in either mode; <strong>Discard</strong>
     (after a confirmation) restores the last saved version. If the file changed on disk,
     <strong>Reload</strong> re-syncs from disk (or Save to overwrite).</p>
  <p><strong>Full screen</strong> (bottom-right ⤢, or press <kbd>f</kbd> outside the
     editor): fill the window with the source and preview; <kbd>f</kbd> or <kbd>Esc</kbd>
     to leave.</p>
  <p><strong>Zen mode</strong> (the tab-row ◎ button, or <kbd>z</kbd> outside the editor): full screen, plus the
     side pane steps aside so you work with the editor alone. <kbd>z</kbd> or <kbd>Esc</kbd>
     to leave — your pane and panel choices come back exactly as they were.</p>
`;

const GRAPH_HELP = `
  <p><strong>Click a node</strong> (its label or details) to inspect the source it
     was produced from and a rendered preview.</p>
  <p><strong>Jump to source</strong> — the icon beside a node's title (in the details panel)
     opens the Source tab with the node's text selected, or, for a synthetic node the compiler
     inserted, places the cursor where it belongs.</p>
  <p><strong>Click a node's circle</strong> to collapse or expand its children.</p>
  <p><strong>Drag</strong> to pan, <strong>scroll</strong> to zoom, and
     <strong>drag the divider</strong> to resize the detail panel — or use its
     <strong>hide handle</strong> to collapse the panel and give the graph the full width
     (click again to bring it back).</p>
  <p><strong>Zoom controls</strong> (bottom-right): <kbd>+</kbd> / <kbd>−</kbd> to
     zoom, type a percentage for an exact ratio, and use <kbd>↺</kbd> to reset the view.</p>
  <p><strong>Full screen</strong> (the bottom-right ⤢ button, or press <kbd>f</kbd>):
     fill the window with the graph; <kbd>f</kbd> or <kbd>Esc</kbd> to leave.
     <strong>Zen mode</strong> (the tab-row ◎ button or <kbd>z</kbd>) goes further, hiding the details panel so the
     graph is alone; <kbd>z</kbd> or <kbd>Esc</kbd> restores your layout.</p>
  <p><strong>Hover a legend entry</strong> (top-right) to highlight its nodes;
     <strong>click</strong> it to dim or show that type. The count shows how many
     are present.</p>
  <p><strong>Arrow keys</strong> move the selection: <kbd>→</kbd> first child,
     <kbd>←</kbd> parent, <kbd>↑</kbd>/<kbd>↓</kbd> siblings; <kbd>Enter</kbd> or
     <kbd>Space</kbd> collapses or expands.</p>
`;

const SEMANTIC_HELP = `
  <p><strong>Semantic model.</strong> The <strong>scene tree</strong> is the graph on the
     left — each scene expands to the <strong>script blocks</strong> it contains (click a
     node's circle to collapse or expand). The <strong>speaker</strong>, <strong>anchor</strong>,
     and <strong>jump-resolution</strong> tables are stacked on the right.</p>
  <p><strong>Cross-linking:</strong> hover a scene, a speaker, or a jump — in the graph or any
     table — to highlight it everywhere it appears, so you can see which scene a jump resolves
     to, or every line a speaker speaks.</p>
  <p><strong>Node details:</strong> click a scene or block in the tree to see its source and a
     rendered preview in the <strong>Node details</strong> panel, pinned to the top of the right
     column. The <strong>Jump to source</strong> icon beside a node's title opens the Source tab
     with that node's text selected.</p>
  <p><strong>Tables column:</strong> drag the divider to resize it, or use the handle on the
     divider to hide the whole column and give the graph full width. Each table (and the node
     details) also <strong>collapses</strong> to a title strip on its own header bar. The
     choices persist.</p>
  <p>The graph pans, zooms, folds, and goes full screen like the other tabs.
     <strong>Zen mode</strong> (the tab-row ◎ button or <kbd>z</kbd>) hides the tables column so the scene tree is
     alone, without disturbing your saved column and table choices.</p>
`;

const EXPLORER_HELP = `
  <p><strong>Browse the project.</strong> The tree on the left lists your
     <kbd>.dialogue.md</kbd> scripts and their folders. <strong>Click a script</strong> to open
     its report; <strong>click a folder</strong> to expand or collapse it.</p>
  <p><strong>Create.</strong> The toolbar's <strong>New file</strong> and
     <strong>New folder</strong> buttons add them at the project root — or
     <strong>right-click a folder</strong> for <strong>New File</strong> / <strong>New Folder</strong>
     inside it. Type the name and press <kbd>Enter</kbd> (<kbd>Esc</kbd> cancels); a script keeps
     its <kbd>.dialogue.md</kbd> ending automatically. A new script opens straight in
     <strong>Edit</strong>, so you can start writing.</p>
  <p><strong>Rename.</strong> <strong>Right-click a script or folder</strong> and choose
     <strong>Rename</strong>, then edit the name and press <kbd>Enter</kbd>.</p>
  <p><strong>Refresh &amp; collapse.</strong> <strong>Refresh</strong> picks up files added or
     changed on disk while keeping your expanded folders open; <strong>Collapse folders</strong>
     closes the whole tree.</p>
  <p><strong>Configuration.</strong> A <kbd>dialogue.toml</kbd> beside your scripts appears as a
     pinned entry that opens in the <strong>Config</strong> tab once a script is open.</p>
`;

const PLAYBOOK_HELP = `
  <p>The <strong>playbook</strong> is what your script compiles to: the JSON a game runtime
     loads and plays. It is generated, so this editor is read-only — change the script, not
     the playbook.</p>
  <p><strong>Reading it.</strong> Fold a section with the arrow in the gutter, and search with
     <kbd>Cmd/Ctrl</kbd>+<kbd>F</kbd>. <strong>Hover a property</strong> to see what the format
     says it means, taken from the published schema; the stretch that description covers is
     washed in while the tip is open.</p>
  <p><strong>Beside it</strong> are three panels — the playbook's header, its speakers, and the
     anchors a jump may name. Each folds from its caret, counts its rows, and searches from the
     magnifier, the same way the Semantic tab's tables do.</p>
  <p><strong>Kept current.</strong> Saving recompiles the playbook, so it always matches the
     script you last saved. A script with errors compiles no playbook, and the tab says so.</p>
`;

/** What the open panel covers. The button reads "Help"; this is its tooltip, so the context
 * is still available without spending status-line width on it. */
const SUMMARY: Record<HelpContext, string> = {
    source: "Using the Source tab",
    graph: "Using the graph",
    semantic: "Using the Semantic tab",
    playbook: "Using the Playbook tab",
    explorer: "Using the Explorer",
};

const CONTENT: Record<HelpContext, string> = {
    source: SOURCE_HELP,
    graph: GRAPH_HELP,
    semantic: SEMANTIC_HELP,
    playbook: PLAYBOOK_HELP,
    explorer: EXPLORER_HELP,
};

/**
 * Show help relevant to the active view: the Source tab explains the source and preview panes,
 * a graph tab explains graph navigation, and the empty state explains the Explorer. Keeps the
 * footer help focused on what the reader is actually looking at.
 */
export function setHelp(context: HelpContext): void {
    const summary = document.getElementById("help-summary");
    const content = document.getElementById("help-content");
    // The button is a glyph, so its tooltip carries both what it is and which panel it opens.
    summary?.closest("button")?.setAttribute("title", `Help — ${SUMMARY[context]}`);
    if (content) content.innerHTML = CONTENT[context];
}

/** The help panel's body, mounted as one panel of the footer drawer. */
export function helpBody(): HTMLElement | null {
    return document.getElementById("help-content");
}
