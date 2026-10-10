/** @file The Source editor's language: optional YAML front matter followed by Markdown. */

import { markdown } from "@codemirror/lang-markdown";
import { yamlFrontmatter } from "@codemirror/lang-yaml";

/**
 * The Source editor's document language: an optional YAML front-matter block followed by the
 * Markdown body. It already includes `markdown()`, so callers do not add it again.
 */
export const sourceLanguage = yamlFrontmatter({ content: markdown() });
