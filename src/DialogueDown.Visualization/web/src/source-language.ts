import { markdown } from "@codemirror/lang-markdown";
import { yamlFrontmatter } from "@codemirror/lang-yaml";

/**
 * The Source editor's document language: an optional canonical YAML front-matter region followed
 * by the existing Markdown body support. The wrapper already carries the inner Markdown
 * extensions, so callers must not install `markdown()` separately.
 */
export const sourceLanguage = yamlFrontmatter({ content: markdown() });
