import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

// Shared knobs for the live e2e: the temp document the server watches and the
// fixed loopback port. `serve.mjs` (the Playwright webServer) and the spec both
// derive the document path from this directory so they always agree.
const here = dirname(fileURLToPath(import.meta.url));

export const LIVE_PORT = 5177;
export const LIVE_DOC = join(here, ".live-doc.dialogue.md");
export const INITIAL_SOURCE = "# Original Scene\n\nAlice: The original line.\n";

// A second live server for the --render-root path: the document sits in a
// sub-folder and references an image in a sibling folder (outside its own), so the
// server must host the common ancestor and serve the report at the sub-path.
export const RENDER_ROOT_PORT = 5178;
export const RENDER_ROOT_TREE = join(here, ".render-root");
export const RENDER_ROOT_DOC = join(RENDER_ROOT_TREE, "proj", "scene.dialogue.md");
export const RENDER_ROOT_IMAGE = join(RENDER_ROOT_TREE, "shared", "out.png");
export const RENDER_ROOT_SOURCE = "# Gallery\n\n![an outside picture](../shared/out.png)\n";

// A shell server over a small tree: a script at the root and one in a
// sub-folder, so the e2e can browse the tree from the empty state's Explorer,
// descend into the folder, and open either script's report.
export const SHELL_PORT = 5179;
export const SHELL_TREE = join(here, ".shell-tree");
export const SHELL_TOP_DOC = join(SHELL_TREE, "top.dialogue.md");
export const SHELL_SUB_DOC = join(SHELL_TREE, "sub", "nested.dialogue.md");
export const SHELL_TOP_SOURCE = "# Top Scene\n\nAlice: The script at the root.\n";
export const SHELL_SUB_SOURCE = "# Nested Scene\n\nBob: The script in a sub-folder.\n";

// A live-edit server: the browser edits the buffer and saves it back to this file.
export const LIVE_EDIT_PORT = 5180;
export const LIVE_EDIT_DOC = join(here, ".live-edit-doc.dialogue.md");
export const LIVE_EDIT_SOURCE = "# Scene\n\nAlice: The first line.\n";

// A config-edit server: a script WITH a `dialogue.toml`, so the Config tab is an editable
// TOML editor. The browser edits the config and saves it back, recompiling the speakers.
export const CONFIG_EDIT_PORT = 5181;
export const CONFIG_EDIT_TREE = join(here, ".config-edit-tree");
export const CONFIG_EDIT_DOC = join(CONFIG_EDIT_TREE, "scene.dialogue.md");
export const CONFIG_EDIT_TOML = join(CONFIG_EDIT_TREE, "dialogue.toml");
export const CONFIG_EDIT_SOURCE =
    "# Scene\n\nAlice: Read <https://example.com/road> before leaving.\n\n" +
    "| Rumor | Source |\n| --- | --- |\n| The bridge is out | The miller |\n";
export const CONFIG_EDIT_CONFIG =
    '[[speakers]]\nname = "Alice"\nid = "A"\n\n' + '[markdown.unmodeled]\nautolink = "ignore"\n';

// A config-create server: a script with NO `dialogue.toml`, served in --edit, so the Config
// tab shows the no-config state with the "Create dialogue.toml" call to action. The tree is
// isolated (its own folder) so creating a config there does not touch the source tree.
export const CONFIG_CREATE_PORT = 5182;
export const CONFIG_CREATE_TREE = join(here, ".config-create-tree");
export const CONFIG_CREATE_DOC = join(CONFIG_CREATE_TREE, "scene.dialogue.md");
export const CONFIG_CREATE_TOML = join(CONFIG_CREATE_TREE, "dialogue.toml");
export const CONFIG_CREATE_SOURCE = "# Scene\n\nAlice: Hello.\n";

// A config-adopt server: like config-create (no `dialogue.toml` at launch, served in --edit) but
// the spec drops a *different* pre-existing config on disk before clicking Create. The server then
// adopts that file instead of failing, so the no-config session recovers into the existing config.
export const CONFIG_ADOPT_PORT = 5184;
export const CONFIG_ADOPT_TREE = join(here, ".config-adopt-tree");
export const CONFIG_ADOPT_DOC = join(CONFIG_ADOPT_TREE, "scene.dialogue.md");
export const CONFIG_ADOPT_TOML = join(CONFIG_ADOPT_TREE, "dialogue.toml");
export const CONFIG_ADOPT_SOURCE = "# Scene\n\nAlice: Hello.\n";
export const CONFIG_ADOPT_CONFIG = '[[speakers]]\nname = "Zelda"\nid = "Z"\n';

// A config-adopt-invalid server: like config-adopt, but the pre-existing `dialogue.toml` dropped on
// disk is *invalid* TOML. Adopting it must still recover the session into an editable Config
// (saved-invalid) rather than dead-ending config-less, so the writer can fix it.
export const CONFIG_ADOPT_INVALID_PORT = 5185;
export const CONFIG_ADOPT_INVALID_TREE = join(here, ".config-adopt-invalid-tree");
export const CONFIG_ADOPT_INVALID_DOC = join(CONFIG_ADOPT_INVALID_TREE, "scene.dialogue.md");
export const CONFIG_ADOPT_INVALID_TOML = join(CONFIG_ADOPT_INVALID_TREE, "dialogue.toml");
export const CONFIG_ADOPT_INVALID_SOURCE = "# Scene\n\nAlice: Hello.\n";
export const CONFIG_ADOPT_INVALID_CONFIG = "[[speakers]]\nbogus = true\n";

// Semantic-autocomplete owns its document because the test replaces the editor buffer.
// Sharing LIVE_EDIT_DOC let this spec race live-edit.spec.ts when Playwright ran files in
// parallel.
export const SEMANTIC_AUTOCOMPLETE_PORT = 5183;
export const SEMANTIC_AUTOCOMPLETE_DOC = join(here, ".semantic-autocomplete.dialogue.md");
export const SEMANTIC_AUTOCOMPLETE_SOURCE = "# Scene\n\nAlice: The first line.\n";

// A script-switch server: a tree with two scripts under one root, served in --edit with the
// first already open. The spec opens the other one from the Explorer, which replaces the report's
// contents instead of the page. It owns its server because a switch changes the server's active
// document, which every other spec on a shared server would see.
export const SWITCH_PORT = 5186;
export const SWITCH_TREE = join(here, ".switch-tree");
export const SWITCH_FIRST_DOC = join(SWITCH_TREE, "first.dialogue.md");
export const SWITCH_SECOND_DOC = join(SWITCH_TREE, "act", "second.dialogue.md");
export const SWITCH_FIRST_SOURCE = "# First Scene\n\nAlice: The script at the root.\n";
export const SWITCH_SECOND_SOURCE = "# Second Scene\n\nBob: The script in a sub-folder.\n";
