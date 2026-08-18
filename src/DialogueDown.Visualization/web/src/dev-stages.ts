import type { Stage } from "./model";

/** A sample document shown in the dev server's Source tab. */
export const DEV_SOURCE = `# Chapter 1

Alice: Hello, **traveler**! Head to [the market](#market).

## Market

Bob: Welcome to my shop. \`OpenShop("Bob")\`
`;

/** A representative sample used by the dev server (never shipped in the build). */
export const DEV_STAGES: Stage[] = [
    {
        title: "Markdown AST",
        description: "The Markdown tree the front end parses from the source.",
        nodes: [
            {
                id: "n0",
                label: "Document",
                attributes: [],
                category: "document",
                source: "# Chapter 1\n\nAlice: Hello, **traveler**! `hasKey`",
            },
            {
                id: "n1",
                label: "Heading (H1)",
                attributes: [
                    { name: "level", value: "1" },
                    { name: "span", value: "[0, 11)" },
                ],
                category: "structure",
                source: "# Chapter 1",
            },
            {
                id: "n2",
                label: "Paragraph",
                attributes: [{ name: "span", value: "[13, 48)" }],
                category: "speech",
                source: "Alice: Hello, **traveler**! `hasKey`",
            },
            {
                id: "n3",
                label: "Text",
                attributes: [
                    { name: "text", value: "Alice: Hello, " },
                    { name: "span", value: "[13, 27)" },
                ],
                category: "text",
                source: "Alice: Hello, ",
            },
            {
                id: "n4",
                label: "Emphasis (Bold)",
                attributes: [{ name: "span", value: "[27, 40)" }],
                category: "styling",
                source: "**traveler**",
            },
            {
                id: "n5",
                label: "Code span",
                attributes: [
                    { name: "content", value: "hasKey" },
                    { name: "span", value: "[41, 49)" },
                ],
                category: "call",
                source: "`hasKey`",
            },
        ],
        edges: [
            { fromId: "n0", toId: "n1", kind: "Child" },
            { fromId: "n0", toId: "n2", kind: "Child" },
            { fromId: "n2", toId: "n3", kind: "Child" },
            { fromId: "n2", toId: "n4", kind: "Child" },
            { fromId: "n2", toId: "n5", kind: "Child" },
        ],
    },
];
