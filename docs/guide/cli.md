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
script. Press **Ctrl + C** in the terminal to stop it.

That is the whole of everyday use. The rest of this page covers the options, which
you can also list at any time with `--help`:

```sh
ddown visualize --help
```

## Two commands

`ddown` has two commands, and the difference is what you get back:

| Command | What it gives you |
| --- | --- |
| `compile` | An answer in the terminal: is this script correct? — and the playbook a game plays |
| `visualize` | A report in your browser that updates while you write. |

## visualize — preview while you write

```sh
ddown visualize [script] [options]
```

**Open a script.** The report opens in your browser, read-only, and refreshes
whenever you save the file:

```sh
ddown visualize my-scene.dialogue.md
```

**Start writing straight away.** `--edit` opens the report ready to edit, so you
can type in the browser and save back to the file. You can also flip between
reading and writing with the View/Edit toggle at any time — `--edit` just saves you
that click each time you start:

```sh
ddown visualize my-scene.dialogue.md --edit
```

**Browse a folder instead of naming a file.** Leave the script out and `ddown` opens
a file explorer over the folder, so you can pick or create a script from the
browser:

```sh
ddown visualize --root my-scripts
```

`--root` is also a boundary: the report can only browse and open files inside that
folder. It defaults to the folder you ran the command in.

**Save a report to send to someone.** `--output` writes one self-contained HTML
file — no server, nothing to install at the other end:

```sh
ddown visualize my-scene.dialogue.md --output report.html
```

### Options

| Option | What it does |
| --- | --- |
| `--edit` | Open ready to edit, instead of read-only. |
| `--root <dir>` | The folder the report may browse. Default: the current folder. |
| `-o`, `--output <path>` | Write a self-contained HTML report and exit. |
| `--config <path>` | Use a specific `dialogue.toml`. Default: the nearest one above the script. |
| `--port <port>` | Serve on a fixed port, instead of any free one. |
| `--no-open` | Don't open a browser (useful in scripts). |

## compile — check a script

```sh
ddown compile <script> [options]
```

**Check for mistakes.** Any problems are printed with the line they came from; the
command exits non-zero when the script has errors, so it fits in a build script:

```sh
ddown compile my-scene.dialogue.md
```

**Keep going after an error.** By default `ddown` stops at the end of the stage
where the first error appeared. `--mode best-effort` pushes on, which surfaces more
problems in one run:

```sh
ddown compile my-scene.dialogue.md --mode best-effort
```

**Save the compiled script for a game to play.** `-o` writes a **playbook**: a JSON
file holding everything a game needs to play the script, and nothing else. Name it
after the script by convention:

```sh
ddown compile my-scene.dialogue.md -o my-scene.playbook.json
```

Leave `-o` off and the playbook goes to standard output, so it can be piped:

```sh
ddown compile my-scene.dialogue.md | jq .anchors
```

A script with errors writes nothing, so a broken compile never leaves a
half-believable file behind.

### Options

| Option | What it does |
| --- | --- |
| `-o`, `--output <path>` | Where to write. Default: standard output. |
| `--emit <format>` | What to write: `playbook` (the default) or `dot` for the compiler's stage graphs. |
| `--mode <mode>` | How far to compile after an error: `stage-boundary` (default) or `best-effort`. |
| `--config <path>` | Use a specific `dialogue.toml`. Default: the nearest one above the script. |

`--emit dot` writes the compiler's internal stage graphs instead, for feeding into
other tools. That's for building tooling rather than writing dialogue, so it lives
in the
[developer docs](https://github.com/pengzhengyi/dialoguedown/blob/main/docs/contributing/design-notes/Compile%20CLI%20-%20Emit%20DOT.md);
`ddown compile --help` lists it too.

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
