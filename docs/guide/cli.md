# Command line

`ddown` is DialogueDown's command-line tool. Use it to **check a script for
mistakes** and to **preview** how DialogueDown reads your script — a report that
opens in your browser and updates as you write. This guide takes you from nothing
to a working `ddown` in a few minutes; it assumes no coding or command-line
background.

> [!NOTE]
> DialogueDown is in early development. Setup is two one-time steps: install .NET,
> then install `ddown`.

## Before you start

You'll type a few commands in a **terminal** — the app that runs commands you type:

- **macOS**: open **Terminal** (Applications → Utilities, or Spotlight-search "Terminal").
- **Windows**: open **Windows Terminal** or **Command Prompt** from the Start menu.
- **Linux**: open your terminal application.

Copy each command below, paste it into the terminal, and press Enter.

## Step 1: Install .NET (once)

`ddown` runs on **.NET**, a free platform from Microsoft. Open
[the .NET download page](https://dotnet.microsoft.com/download) and install the
latest **.NET SDK** for your system (choose the download labeled **SDK**) — it
includes everything `ddown` needs. Microsoft's
[install guide](https://learn.microsoft.com/en-us/dotnet/core/install/) walks
through every platform if you get stuck.

Then confirm it worked:

```sh
dotnet --version
```

You should see a version number (8.0 or higher). If the command isn't found, close
the terminal, open a new one, and try again.

## Step 2: Install ddown (once)

```sh
dotnet tool install --global DialogueDown.Cli
```

This downloads `ddown` and makes it available in every terminal (it installs as a
[.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools)).
Confirm it:

```sh
ddown --help
```

## Step 3: Use it

Point `ddown` at your script. Replace `my-scene.dialogue.md` below with your
script's file name, and run the command **in the same folder as the script** — or
drag the script file into the terminal window to fill in its location for you. (New
to writing one? See [Script language](script-language.md).)

Check a script for mistakes:

```sh
ddown compile my-scene.dialogue.md
```

Preview how DialogueDown reads your script, stage by stage, in your browser:

```sh
ddown visualize my-scene.dialogue.md
```

`visualize` opens the report in your browser and refreshes it as you edit the
script. Press **Ctrl + C** in the terminal to stop it. Add `--help` to any command
to see its options (for example, `ddown visualize --help`).

Export every compiler-stage graph as Graphviz DOT text:

```sh
ddown visualize my-scene.dialogue.md --emit dot -o stages.dot
```

Fenced `mermaid` blocks are authoring aids, not compiler-stage output. The HTML
report renders them directly in its Markdown previews.

## Keep ddown up to date

Get the newest version, or remove it, at any time:

```sh
dotnet tool update --global DialogueDown.Cli
dotnet tool uninstall --global DialogueDown.Cli
```

## Other ways to install

A .NET tool is the first — and, for now, only — way to install `ddown`. Downloads
that don't need .NET (a standalone app, and a Homebrew formula for macOS and Linux)
are under consideration.

## See also

- [Script language](script-language.md) — how to write a script.
- [Project configuration](configuration.md) — the optional `dialogue.toml` settings file.
- [Error codes](error-codes.md) — what the messages from `ddown compile` mean.
