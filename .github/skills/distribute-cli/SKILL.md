---
name: distribute-cli
description: Package, locally test, and publish the DialogueDown `ddown` CLI as a cross-platform .NET global tool on NuGet. Refresh the locally installed tool after a merge to main, and on a changelog release set the semantic version, pack, and push the package. Composes with maintain-oss and keeps docs/guide/cli.md as the user-facing source of truth. Self-contained binaries and a Homebrew tap are a planned second channel.
---

# Distribute the ddown CLI

Package and ship DialogueDown's command-line tool, `ddown`
(`src/DialogueDown.Cli`), as a **.NET global tool** on NuGet — the first
distribution channel. This skill owns the packaging metadata, local testing, and
the release push. It does **not** own the changelog, version
decision, or tag; those belong to `maintain-oss`. Invoke this skill from within
that release flow.

> [!IMPORTANT]
> Publishing to NuGet, creating tags, adding a publishing workflow or its secret, and
> changing the public command name are **sensitive actions**. Never do them without
> explicit user approval, and never commit a NuGet API key. Verify locally first.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Package id** | The NuGet identity users install: `DialogueDown.Cli`. |
| **Tool command** | The command placed on `PATH`: `ddown`. |
| **Local install** | A global tool installed from a local `.nupkg` for testing, not from NuGet. |
| **Release push** | Publishing a semantic-versioned package to NuGet.org. |

The user-facing install and usage contract lives in
[`docs/guide/cli.md`](../../../docs/guide/cli.md). Keep the package id, command
name, and runtime requirements in this skill, that doc, and the CLI code in sync.

## When to invoke

| Trigger | Do this | Section |
| --- | --- | --- |
| **First time** — the CLI is not yet packable | Add the tool packaging + metadata, then verify locally | [Package](#1-package-the-cli-as-a-net-tool), [Test locally](#2-test-the-tool-locally) |
| **After a PR merges to main** | Refresh the maintainer's locally installed `ddown` so it reflects merged changes (no publishing) | [Refresh the local tool](#refresh-the-local-tool-after-a-merge) |
| **On a changelog release** (a new SemVer section) | Set the version, pack, and push to NuGet | [Release push](#3-release-push-approval-gated) |
| **Reaching more users later** | Add self-contained binaries + a Homebrew tap | [Planned second channel](#planned-second-channel-self-contained-binaries) |

## Prerequisites

- The **.NET 8 SDK** (`global.json` pins the floor). `dotnet pack` and
  `dotnet tool` are part of the SDK.
- For a release push: a **NuGet.org account** and an **API key** (NuGet.org →
  *Account* → *API Keys*), provided as the `NUGET_API_KEY` environment variable or
  a repository secret. Never hard-code or commit it.

`ddown` depends on `DialogueDown.Visualization.Live`, which is built on ASP.NET
Core, so the packaged tool is **framework-dependent** and needs the **ASP.NET Core
runtime** for `ddown visualize` (bundled with the SDK). This matches the requirements in
`docs/guide/cli.md`; keep both statements aligned.

## 1. Package the CLI as a .NET tool

Add these to `src/DialogueDown.Cli/DialogueDown.Cli.csproj` (the project already
sets `OutputType=Exe` and `<Version>`):

```xml
<PropertyGroup>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>ddown</ToolCommandName>
  <PackageId>DialogueDown.Cli</PackageId>

  <Authors>Zhengyi Peng</Authors>
  <Description>Compile and visualize DialogueDown dialogue scripts from the command line.</Description>
  <PackageTags>dialogue;markdown;compiler;narrative;gamedev;cli</PackageTags>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/pengzhengyi/godot-dialoguedown</PackageProjectUrl>
  <RepositoryUrl>https://github.com/pengzhengyi/godot-dialoguedown.git</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>

<ItemGroup>
  <None Include="..\..\README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

Also rename the command so code and package agree:

- In `src/DialogueDown.Cli/CliConfigurator.cs`, change
  `config.SetApplicationName("dialoguedown")` to `config.SetApplicationName("ddown")`
  so `--help`/usage read `ddown …`.
- Update `README.md` (the run example) and any docs that still say
  `dotnet run --project src/DialogueDown.Cli --` to the installed `ddown` command,
  and link `docs/guide/cli.md`.

Then build the package:

```sh
dotnet pack src/DialogueDown.Cli/DialogueDown.Cli.csproj -c Release -o ./artifacts
```

This writes `./artifacts/DialogueDown.Cli.<version>.nupkg`. Add `artifacts/` to
`.gitignore` if it is not already ignored.

## 2. Test the tool locally

Install the packed tool from the local folder — no NuGet involved — and exercise
the real commands:

```sh
dotnet tool install --global --add-source ./artifacts DialogueDown.Cli
ddown --version
ddown --help
ddown compile examples/gallery.dialogue.md
ddown visualize examples/gallery.dialogue.md --no-open   # prints the URL
```

Iterate and clean up:

```sh
dotnet tool update  --global --add-source ./artifacts DialogueDown.Cli
dotnet tool uninstall --global DialogueDown.Cli
```

> [!TIP]
> To keep a machine clean, test as a **local tool** in a scratch directory instead:
> `dotnet new tool-manifest`, then
> `dotnet tool install --add-source ./artifacts DialogueDown.Cli`, and invoke it
> with `dotnet ddown …`.

Confirm the package contents are sane before any push:

```sh
unzip -l ./artifacts/DialogueDown.Cli.<version>.nupkg   # expect the CLI + its deps, README, and the ddown tool settings
```

### Refresh the local tool after a merge

After a PR merges to `main`, give the maintainer's installed `ddown` the merged
changes without publishing: from an up-to-date `main`, re-pack and update the
global tool.

```sh
git switch main && git pull --ff-only
dotnet pack src/DialogueDown.Cli/DialogueDown.Cli.csproj -c Release -o ./artifacts
dotnet tool update --global --add-source ./artifacts DialogueDown.Cli
ddown --version
```

`dotnet tool update` needs a version newer than the installed one. During
same-version iteration, `uninstall` then `install`, or pack with a bumped
`--version-suffix` (for example `-p:VersionSuffix=dev.$(date +%s)`).

## 3. Release push (approval-gated)

Run only when `maintain-oss` has cut a changelog release and the user has approved
publishing. `maintain-oss` owns the version choice, the dated `CHANGELOG.md`
heading, and the git tag; this skill packs that version and pushes it.

1. **Set the version** to the release's SemVer — either bump `<Version>` in the
   csproj to match the changelog heading, or pass it at pack time:

   ```sh
   dotnet pack src/DialogueDown.Cli/DialogueDown.Cli.csproj -c Release -o ./artifacts -p:Version=<x.y.z>
   ```

2. **Re-verify locally** (section 2) against the release build.

3. **Push to NuGet** (needs approval; `NUGET_API_KEY` must be set in the
   environment, never committed):

   ```sh
   dotnet nuget push ./artifacts/DialogueDown.Cli.<x.y.z>.nupkg \
     --api-key "$NUGET_API_KEY" \
     --source https://api.nuget.org/v3/index.json \
     --skip-duplicate
   ```

4. **Confirm** the package appears at
   `https://www.nuget.org/packages/DialogueDown.Cli` and installs cleanly:
   `dotnet tool install --global DialogueDown.Cli`.

> [!TIP]
> To automate this, add a GitHub Actions release workflow triggered on a `v*` tag
> that packs and pushes with the key stored as the `NUGET_API_KEY` repository
> secret. Adding a publishing workflow **and** its secret is a sensitive action:
> propose it and wait for approval before committing the workflow or creating the
> secret.

## Planned second channel: self-contained binaries

For users without .NET installed (channel **B**, sequenced after NuGet ships):
publish per-platform, self-contained single-file executables and attach them to a
GitHub Release, then offer a Homebrew tap for macOS and Linux.

```sh
# one per runtime id: osx-arm64, osx-x64, linux-x64, win-x64
dotnet publish src/DialogueDown.Cli/DialogueDown.Cli.csproj \
  -c Release -r <rid> --self-contained true -p:PublishSingleFile=true -o ./artifacts/<rid>
```

Docker (channel **D**) is **out of scope** by decision: `ddown` is an author-time
tool whose `visualize` opens a local browser, so a container adds little. Revisit
only if a headless `ddown compile` CI use case emerges.

## Guardrails

- **Approval-gated:** the NuGet push, git tags, any publishing workflow or secret,
  and renaming the public `ddown` command. Ask first.
- **Never commit secrets** — the NuGet API key stays in the environment or a
  repository secret.
- **Verify before publish** — always install and run the packed tool against the
  release build first.
- **Keep the contract in sync** — the package id, `ddown` command name, and
  runtime requirements must match across this skill, `docs/guide/cli.md`, and the
  CLI code.
- **Compose, do not duplicate** — defer the changelog, version, and tag to
  `maintain-oss`; run `polish-tech-doc` when editing `docs/guide/cli.md` or this
  skill.
