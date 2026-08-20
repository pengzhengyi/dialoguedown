import { defineConfig, type Plugin } from "vitest/config";
import { copyFileSync, existsSync, renameSync } from "node:fs";
import { createRequire } from "node:module";
import { resolve } from "node:path";

// Rename the report build artifact to report.html — a clearer name than Vite's
// default index.html for the file the .NET library embeds. Done in a plugin (not
// a shell `mv`) so it is portable across platforms.
function renameToReport(): Plugin {
    return {
        name: "rename-to-report",
        closeBundle() {
            const index = resolve("dist", "index.html");
            if (existsSync(index)) renameSync(index, resolve("dist", "report.html"));
        },
    };
}

// Mermaid ships a self-contained build that assigns `globalThis.mermaid`, which is exactly what a
// report needs: one file to fetch when a script shows its first diagram, or to inline when such a
// script is exported. Copying that build verbatim keeps it out of the client bundle — importing it
// would pull its diagram types, cytoscape, and katex into the page every reader loads.
function copyMermaidBuild(): Plugin {
    return {
        name: "copy-mermaid-build",
        closeBundle() {
            const require = createRequire(import.meta.url);
            copyFileSync(
                require.resolve("mermaid/dist/mermaid.min.js"),
                resolve("dist", "mermaid.js"),
            );
        },
    };
}

// The report ships as a small page plus two constant assets the .NET library embeds: the client
// script and its stylesheet. Serving links them so a browser downloads and compiles them once for
// every script it opens; exporting inlines them so the file works offline. Fonts and icons are
// inlined into the stylesheet rather than emitted beside it, so both shapes need only these three.
export default defineConfig({
    root: ".",
    plugins: [renameToReport(), copyMermaidBuild()],
    build: {
        target: "es2022",
        outDir: "dist",
        emptyOutDir: true,
        // Above the largest icon font, so no asset is emitted as a separate file.
        assetsInlineLimit: 262_144,
        rollupOptions: {
            input: resolve("index.html"),
            output: {
                entryFileNames: "report.js",
                assetFileNames: "report.[ext]",
            },
        },
    },
    test: {
        environment: "jsdom",
        include: ["src/**/*.test.ts"],
        coverage: {
            provider: "v8",
            include: ["src/**/*.ts"],
            // Test files, dev-only helpers, and the browser-integration modules that
            // are exercised end-to-end by Playwright (d3 layout, tippy, wiring) rather
            // than by jsdom unit tests.
            exclude: [
                "src/**/*.test.ts",
                "src/dev-stages.ts",
                "src/main.ts",
                "src/model.ts",
                "src/app.ts",
                "src/tree-view.ts",
                "src/tooltips.ts",
                "src/source-view.ts",
                "src/semantic-view.ts",
                "src/live-edit-ui.ts",
            ],
        },
    },
});
