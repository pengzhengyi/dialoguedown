import js from "@eslint/js";
import jsdoc from "eslint-plugin-jsdoc";
import globals from "globals";
import tseslint from "typescript-eslint";

export default tseslint.config(
    {
        ignores: ["dist/**", "node_modules/**", "playwright-report/**", "test-results/**"],
    },
    js.configs.recommended,
    ...tseslint.configs.recommended,
    {
        files: ["src/**/*.ts", "e2e/**/*.ts", "e2e-live/**/*.ts", "*.config.ts"],
        languageOptions: {
            globals: {
                ...globals.browser,
                ...globals.node,
            },
        },
        rules: {
            // Every import sits at the top of the module, before any declaration, so a reader
            // sees all of a module's dependencies in one place.
            "no-restricted-syntax": [
                "error",
                {
                    selector: "Program > :not(ImportDeclaration) ~ ImportDeclaration",
                    message: "Move this import up with the module's other imports.",
                },
            ],
        },
    },
    {
        // Every client module opens with a `@file` comment saying what it does, so a reader
        // learns what a file is for before reading its code.
        files: ["src/**/*.ts"],
        ignores: ["src/**/*.test.ts", "src/**/*.d.ts"],
        plugins: { jsdoc },
        rules: {
            "jsdoc/require-file-overview": "error",
        },
    },
    {
        // Node scripts (the live e2e webServer launcher).
        files: ["e2e-live/**/*.mjs"],
        languageOptions: {
            globals: { ...globals.node },
        },
    },
);
