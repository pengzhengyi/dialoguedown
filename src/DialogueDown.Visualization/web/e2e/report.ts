import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import type { Report, Stage } from "../src/model";

const here = path.dirname(fileURLToPath(import.meta.url));
const dist = path.join(here, "..", "dist");
const read = (name: string): string => fs.readFileSync(path.join(dist, name), "utf8");

/** The fence rule the preview and the .NET exporter both use: the info string's first word. */
function drawsADiagram(source: string | undefined): boolean {
    if (!source) return false;
    return source
        .split(/\r?\n/)
        .some((line) => /^ {0,3}(?:`{3,}|~{3,})\s*mermaid(?:\s|$)/i.test(line));
}

/**
 * Assemble the built report exactly as the .NET library does when it exports a self-contained
 * file: inline the client and its stylesheet, inject the sample data into the __REPORT__ slot,
 * and carry Mermaid only for a script that draws a diagram. Written to a temp file and returned
 * as a file:// URL, so the suite exercises the shape a reader actually opens from disk.
 */
export function writeReport(report: Report): string {
    let html = read("report.html")
        .replace('"__MERMAID__"', () => '""')
        .replace(
            '<link rel="stylesheet" crossorigin href="/report.css">',
            () => `<style>${read("report.css")}</style>`,
        );
    if (drawsADiagram(report.source)) {
        html = html.replace("</head>", () => `<script>${read("mermaid.js")}</script></head>`);
    }

    html = html
        .replace(
            '<script type="module" crossorigin src="/report.js"></script>',
            () => `<script type="module">${read("report.js")}</script>`,
        )
        // Function replacement avoids `$` being treated specially in the JSON.
        .replace('"__REPORT__"', () => JSON.stringify(report));
    const out = path.join(os.tmpdir(), `dd-report-${process.pid}-${Date.now()}.html`);
    fs.writeFileSync(out, html);
    return pathToFileURL(out).href;
}

/** A sample exercising every node category (document, structure, …, break). */
export const SAMPLE_STAGES: Stage[] = [
    {
        title: "Markdown AST",
        description: "The Markdown tree the front end parses from the source.",
        nodes: [
            {
                id: "n0",
                label: "Document",
                attributes: [],
                category: "document",
                source: "---\ntitle: Demo\n---\n\n# Scene",
            },
            {
                id: "n1",
                label: "Heading (H1)",
                attributes: [
                    { name: "level", value: "1" },
                    { name: "span", value: "[0, 7)" },
                ],
                category: "structure",
                source: "# Scene",
            },
            {
                id: "n2",
                label: "Paragraph",
                attributes: [{ name: "span", value: "[9, 60)" }],
                category: "speech",
                source: "Alice: Look! ![A painting](x.jpg) It is *lovely*.",
            },
            {
                id: "n3",
                label: "Text",
                attributes: [
                    {
                        name: "text",
                        value: "Alice: Look at this very long line that should be ellipsised on the node",
                    },
                    { name: "span", value: "[9, 21)" },
                ],
                category: "text",
                source: "Alice: Look ",
            },
            {
                id: "n4",
                label: "Image",
                attributes: [
                    { name: "source", value: "x.jpg" },
                    { name: "alt", value: "A painting" },
                    { name: "span", value: "[21, 45)" },
                ],
                category: "media",
                source: "![A painting](x.jpg)",
            },
            {
                id: "n5",
                label: "Emphasis (Italic)",
                attributes: [{ name: "span", value: "[46, 54)" }],
                category: "styling",
                source: "*lovely*",
            },
            {
                id: "n6",
                label: "Link",
                attributes: [
                    { name: "target", value: "#scene-2" },
                    { name: "label", value: "Continue" },
                    { name: "span", value: "[60, 82)" },
                ],
                category: "jump",
                source: "[Continue](#scene-2)",
            },
            {
                id: "n7",
                label: "List (unordered)",
                attributes: [{ name: "span", value: "[83, 110)" }],
                category: "choice",
                source: "- Ask more\n- Leave",
            },
            {
                id: "n8",
                label: "Code span",
                attributes: [
                    { name: "content", value: 'JoinClub("Alice")' },
                    { name: "span", value: "[111, 130)" },
                ],
                category: "call",
                source: '`JoinClub("Alice")`',
            },
            {
                id: "n9",
                label: "Line break (soft)",
                attributes: [{ name: "span", value: "[54, 55)" }],
                category: "break",
                source: "\n",
            },
        ],
        edges: [
            { fromId: "n0", toId: "n1", kind: "Child" },
            { fromId: "n0", toId: "n2", kind: "Child" },
            { fromId: "n0", toId: "n7", kind: "Child" },
            { fromId: "n0", toId: "n8", kind: "Child" },
            { fromId: "n2", toId: "n3", kind: "Child" },
            { fromId: "n2", toId: "n4", kind: "Child" },
            { fromId: "n2", toId: "n5", kind: "Child" },
            { fromId: "n2", toId: "n9", kind: "Child" },
            { fromId: "n2", toId: "n6", kind: "Child" },
        ],
    },
];

// Enough filler paragraphs that the "## The Market" heading sits below the
// preview pane's initial fold, so clicking its anchor link is an observable scroll.
const filler = Array.from(
    { length: 14 },
    (_, i) => `Paragraph ${i + 1}: the gallery is calm and quiet today.`,
).join("\n\n");

/** A sample source document with front matter, a heading, and an in-document
 *  anchor link — exercises the Source tab preview and anchor navigation. */
export const SAMPLE_SOURCE = `---
title: Demo
---

# Scene

Alice: Look at this painting. Jump to [the market](#the-market).

${filler}

## The Market

Bob: Welcome to the market!
`;

/** The full sample report: the source document plus its Markdown AST stage. */
export const SAMPLE_REPORT: Report = { source: SAMPLE_SOURCE, stages: SAMPLE_STAGES };
