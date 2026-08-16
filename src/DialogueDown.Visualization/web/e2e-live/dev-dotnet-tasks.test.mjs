import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const tasks = JSON.parse(
    readFileSync(resolve(here, "../../../../.vscode/tasks.json"), "utf8"),
).tasks;
const repositoryRoot = resolve(here, "../../../..");
const normalTestCommand = /dotnet test DialogueDown\.sln --configuration Release --no-build -m:3/;
const guidance = [
    "README.md",
    "CONTRIBUTING.md",
    "AGENTS.md",
    ".github/copilot-instructions.md",
    ".github/instructions/csharp.instructions.md",
    ".github/pull_request_template.md",
].map((path) => [path, readFileSync(resolve(repositoryRoot, path), "utf8")]);

test("the fast .NET build skips analyzers without replacing the full build", () => {
    const fast = tasks.find((task) => task.label === "build: fast");
    const full = tasks.find((task) => task.label === "build");

    assert.ok(fast);
    assert.match(fast.command, /RunAnalyzers=false/);
    assert.match(fast.command, /--no-restore/);
    assert.notDeepEqual(fast.group, { kind: "build", isDefault: true });

    assert.ok(full);
    assert.doesNotMatch(full.command, /RunAnalyzers=false/);
    assert.deepEqual(full.group, { kind: "build", isDefault: true });
});

test("targeted .NET tasks select a project and optional filter", () => {
    const project = tasks.find((task) => task.label === "test: project");
    const filtered = tasks.find((task) => task.label === "test: filter");

    assert.ok(project);
    assert.match(project.command, /\$\{input:dotnetTestProject\}/);
    assert.match(project.command, /--no-build/);
    assert.match(project.command, /--no-restore/);

    assert.ok(filtered);
    assert.match(filtered.command, /\$\{input:dotnetTestProject\}/);
    assert.match(filtered.command, /\$\{input:dotnetTestFilter\}/);
    assert.match(filtered.command, /--filter/);
});

test("local .NET test guidance parallelizes projects while CI and coverage stay serial", () => {
    const full = tasks.find((task) => task.label === "test");
    const coverage = tasks.find((task) => task.label === "coverage");

    assert.ok(full);
    assert.match(full.command, /-m:3/);
    assert.ok(coverage);
    assert.doesNotMatch(coverage.command, /-m:3/);

    for (const [path, content] of guidance) {
        assert.match(content, normalTestCommand, path);
    }

    for (const path of [".github/workflows/ci.yml", ".github/workflows/release.yml"]) {
        const workflow = readFileSync(resolve(repositoryRoot, path), "utf8");
        assert.match(workflow, /dotnet test DialogueDown\.sln --configuration Release --no-build/);
        assert.doesNotMatch(workflow, /dotnet test DialogueDown\.sln[^\n]*-m:3/);
    }
});

test("package versions are managed centrally, without pinning transitives into the published packages", () => {
    // Two projects cannot drift onto different versions of one package if only one file states
    // versions — the failure that left a test project on its own test runner (#287).
    const central = readFileSync(resolve(repositoryRoot, "Directory.Packages.props"), "utf8");
    assert.match(central, /<ManagePackageVersionsCentrally>true<\/ManagePackageVersionsCentrally>/);

    // Transitive pinning promotes pinned transitives into the generated nuspec, so enabling it
    // would widen what DialogueDown and DialogueDown.Cli declare to their consumers — a change to
    // the published packages that nothing else here would surface.
    for (const [path, text] of [
        ["Directory.Packages.props", central],
        [
            "Directory.Build.props",
            readFileSync(resolve(repositoryRoot, "Directory.Build.props"), "utf8"),
        ],
    ]) {
        assert.doesNotMatch(
            text,
            /<CentralPackageTransitivePinningEnabled>\s*true/i,
            `${path} must not pin transitive dependencies: both shipped packages would declare them`,
        );
    }
});
