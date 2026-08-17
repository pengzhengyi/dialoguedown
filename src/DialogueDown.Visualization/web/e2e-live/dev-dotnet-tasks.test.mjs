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
const normalTestCommand = /dotnet test DialogueDown\.sln --configuration Release --no-build/;
const expectedTestFloor = /--minimum-expected-tests \d+/;
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

test("targeted .NET tasks select a project and optional filter, and stop at the first failure", () => {
    const project = tasks.find((task) => task.label === "test: project");
    const filtered = tasks.find((task) => task.label === "test: filter");
    const byClass = tasks.find((task) => task.label === "test: class");

    assert.ok(project);
    assert.match(project.command, /\$\{input:dotnetTestProject\}/);
    assert.match(project.command, /--no-build/);
    assert.match(project.command, /--no-restore/);

    assert.ok(filtered);
    assert.match(filtered.command, /\$\{input:dotnetTestProject\}/);
    assert.match(filtered.command, /\$\{input:dotnetTestFilter\}/);
    assert.match(filtered.command, /--filter/);

    assert.ok(byClass);
    assert.match(byClass.command, /\$\{input:dotnetTestClass\}/);
    assert.match(byClass.command, /--filter-class/);

    // Fail-fast is the point of an inner-loop run: the first failure is the one being worked on.
    // `--stop-on-fail` takes an explicit `on`/`off`; passing it bare is itself a "Zero tests ran".
    for (const task of [project, filtered, byClass]) {
        assert.match(task.command, /--stop-on-fail on\b/, task.label);
    }
});

test("the documented .NET test commands guard against a silent zero-test run", () => {
    // Microsoft Testing Platform forwards an argument it does not recognize to the test app, and
    // an app that rejects one exits without running anything. The run then reports "Zero tests
    // ran" and looks like an ordinary command — `-m:3` (an MSBuild-only switch), a bare
    // `--stop-on-fail` (it needs `on`/`off`), and `--maximum-failed-tests` (not offered here) all
    // land the same way. Banning flags one at a time cannot catch the next one, so every
    // documented full command states how many tests it expects: too few is then a loud failure
    // (exit code 9) rather than a green-looking no-op. Raise or lower the floor deliberately.
    const full = tasks.find((task) => task.label === "test");
    const coverage = tasks.find((task) => task.label === "coverage");

    assert.ok(full);
    assert.match(full.command, expectedTestFloor);
    assert.ok(coverage);
    assert.match(coverage.command, expectedTestFloor);

    for (const [path, content] of guidance) {
        assert.match(content, normalTestCommand, path);
        assert.match(content, expectedTestFloor, path);
    }

    for (const path of [".github/workflows/ci.yml", ".github/workflows/release.yml"]) {
        const workflow = readFileSync(resolve(repositoryRoot, path), "utf8");
        assert.match(workflow, /dotnet test DialogueDown\.sln --configuration Release --no-build/);
        assert.match(workflow, expectedTestFloor, path);
    }

    // MSBuild-only switches never reach the test app as options; keep them out by name too.
    const everywhere = [
        ...guidance,
        ["tasks.json", JSON.stringify(tasks)],
        ...[".github/workflows/ci.yml", ".github/workflows/release.yml"].map((path) => [
            path,
            readFileSync(resolve(repositoryRoot, path), "utf8"),
        ]),
    ];
    for (const [path, content] of everywhere) {
        assert.doesNotMatch(content, /-m:3|--maxcpucount/, path);
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

test("the libraries a game references keep the target framework Godot can load", () => {
    // Godot bundles the .NET runtime an exported game runs on, so the floor is Godot's, not ours.
    // Dropping net8.0 from a shipped library would break every Godot project silently — the build
    // stays green and only a consumer's export fails. See the Target Frameworks note.
    for (const project of [
        "DialogueDown",
        "DialogueDown.ConfigurationLoader",
        "DialogueDown.Playbook",
    ]) {
        const csproj = readFileSync(
            resolve(repositoryRoot, `src/${project}/${project}.csproj`),
            "utf8",
        );
        const frameworks = /<TargetFrameworks>([^<]+)<\/TargetFrameworks>/.exec(csproj);

        assert.ok(frameworks, `${project} must multi-target, not pin one framework`);
        const targets = frameworks[1].split(";").map((value) => value.trim());
        assert.ok(
            targets.includes("net8.0"),
            `${project} must keep net8.0 for Godot's bundled runtime; found ${targets.join(", ")}`,
        );
        assert.ok(
            targets.includes("net10.0"),
            `${project} must also offer net10.0 (LTS); found ${targets.join(", ")}`,
        );
    }
});
