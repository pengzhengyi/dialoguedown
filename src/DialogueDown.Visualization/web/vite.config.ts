import { defineConfig, type Plugin } from "vitest/config";
import { existsSync, renameSync } from "node:fs";
import { resolve } from "node:path";
import { viteSingleFile } from "vite-plugin-singlefile";

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

// The report is shipped as ONE self-contained HTML file that the .NET library embeds.
// vite-plugin-singlefile inlines all JS and CSS; the .NET side injects per-page data
// into the page's slot (the report stages, or an empty project shell).
export default defineConfig({
    root: ".",
    plugins: [viteSingleFile(), renameToReport()],
    build: {
        target: "es2022",
        outDir: "dist",
        emptyOutDir: true,
        rollupOptions: {
            input: resolve("index.html"),
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
