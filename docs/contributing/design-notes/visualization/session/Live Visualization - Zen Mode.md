# Live Visualization — Zen Mode

> [!NOTE]
> Status: **implemented**. A deeper form of full screen: the app chrome *and* the active
> tab's secondary panel step aside, leaving the editor alone on Source and Config, and the
> graph alone on the AST and Semantic Model tabs. Press <kbd>z</kbd> to enter, <kbd>z</kbd>
> or <kbd>Esc</kbd> to leave.
>
> Like the rest of the visualization tooling, this surface is "vibe-coded" (see the
> visualization note's maturity caveat); the core engine stays the reviewed surface.

## Table of contents

- [Goal and scope](#goal-and-scope)
- [Ubiquitous language](#ubiquitous-language)
- [Functionality checklist](#functionality-checklist)
- [Key design decisions](#key-design-decisions)
  - [D1 — Zen is a presentation flag, never a state change](#d1--zen-is-a-presentation-flag-never-a-state-change)
  - [D2 — One focus-mode controller, not a second toggle](#d2--one-focus-mode-controller-not-a-second-toggle)
  - [D3 — Zen hides panels, not the content's own tools](#d3--zen-hides-panels-not-the-contents-own-tools)
  - [D4 — A control beside maximize, plus the keyboard](#d4--a-control-beside-maximize-plus-the-keyboard)
- [Error and boundary cases](#error-and-boundary-cases)
- [Integration](#integration)
- [Testability](#testability)
- [Follow-ups](#follow-ups)

## Goal and scope

Full screen already hides the app chrome, but the active tab keeps both of its panes — so
writing a scene still happens in half the window, beside a preview. Zen closes that: it
gives the tab's **primary** content the whole viewport.

What "primary" means per tab:

| Tab | Alone in Zen | Stepped aside |
| --- | --- | --- |
| Source | The editor | The rendered preview |
| Config | The TOML editor | The configured-speakers column |
| AST graphs | The graph | The node-details inspector |
| Semantic Model | The scene tree | The tables column |

The Explorer sidebar steps aside too, on every tab — full screen leaves it up, so this is
Zen's own addition rather than something inherited.

Out of scope: a centered/narrowed editor column (VS Code's optional Zen centering), and a
per-tab memory of "was Zen on" — Zen is a transient reading posture, not a saved layout.

## Ubiquitous language

| Term | Meaning |
| --- | --- |
| **Focus mode** | How much is hidden to concentrate on the active tab: `normal`, `maximized`, or `zen`. |
| **Chrome** | The app header and tab row, the status footer, and the live banner. |
| **Secondary panel** | The pane beside a tab's primary content — preview, speakers, inspector, or tables. |

## Functionality checklist

- [x] <kbd>z</kbd> enters Zen from normal **or** from full screen (it deepens).
- [x] <kbd>z</kbd> or <kbd>Esc</kbd> leaves Zen and returns to normal in one press.
- [x] Zen hides the chrome exactly as full screen does, plus the secondary panel.
- [x] Leaving Zen restores the reader's own collapse choices untouched.
- [x] <kbd>z</kbd> is ignored while typing and when combined with a modifier, so undo and
      the letter `z` are never hijacked.
- [x] A tab-row button enters Zen and shows when it is engaged; the keyboard does the same.
- [x] A visible exit affordance is present, since the header is hidden.

## Key design decisions

### D1 — Zen is a presentation flag, never a state change

Zen could have driven the existing hide/show controllers — calling `toggle()` on the
preview, inspector, and tables panels, then restoring them on exit. It does not, for two
reasons.

Those controllers **persist** to `localStorage`. Driving them would write Zen's transient
posture into the reader's remembered layout, so a crash, a reload, or a missed restore
would leave their preview permanently hidden with no indication why. And restoring
correctly would mean capturing prior state per panel per tab and replaying it — a small
state machine whose only job is to undo itself.

Instead Zen is a class on the root element, and CSS hides the panels directly. The
collapse controllers are never touched, so **exit needs no restore logic at all**: removing
the class reveals whatever the reader had chosen. The reader's own collapse choice also
survives a Zen round trip, which is covered by a test.

The cost is that the Zen rules mirror what each panel's collapsed class already does,
which is duplication in CSS. That is accepted: it is declarative, local, and cannot desync
the way a second source of truth over the same state would.

One consequence needs handling in script rather than CSS. Hiding a panel with
`display: none` does not reliably move keyboard focus out of it, and the collapse toggles
live inside the very dividers Zen hides — so a focused toggle could still be activated by
Enter, collapsing the panel and **persisting** it. Entering Zen therefore blurs the active
element when it sits inside a hidden region, which is what keeps D1's promise true for
keyboard users as well.

### D2 — One focus-mode controller, not a second toggle

Zen and full screen are the same concern at two depths, so they live in one controller with
a single `FocusMode` (`normal` / `maximized` / `zen`) rather than two independent booleans
that could disagree — `zen` without `maximized` would hide panels while leaving the chrome
up. `initFullscreen` gained `toggleZen()`, `isZen()`, and `mode()`; its existing API is
unchanged, so every maximize button, key, and test kept working.

Zen sets the maximized class **as well as** the Zen class, reusing the chrome-hiding rules
instead of restating them, and giving the corner exit chip its behavior for free.

Either key leaves focus mode entirely rather than stepping down a level: from Zen,
<kbd>f</kbd> returns to normal instead of dropping to full screen. One press always gets
the reader all the way back, which is what someone pressing a key to escape actually wants.

### D3 — Zen hides panels, not the content's own tools

The graph keeps its legend and zoom cluster; the editor keeps its gutter and diagnostics.
Those are instruments *for reading the primary content*, not competing panels — hiding them
would make Zen a worse graph rather than a more focused one. The rule is "remove the
neighbours, keep the tools."

### D4 — A control beside maximize, plus the keyboard

Zen gets a button at the right end of the tab row, immediately left of the maximize
control, so the two focus modes read as one cluster. It carries the concentric-circles
codicon (`target`) that VS Code shows beside its own **Zen Mode** command, so the mode is
recognizable to readers who already know it, and it says something different from
maximize's outward arrows sitting beside it rather than looking like a second flavour of
full screen. `layout-centered` was rejected: that is VS Code's glyph for the separate
**Centered Layout** command, so it would name a different feature.

The button shows Zen as **engaged** (`aria-pressed`, in the mode accent) because Zen is a
sticky mode rather than a one-shot action. It tracks Zen specifically, not the shared
chrome-hiding: in plain full screen it is still an *available* action, not an active one.

<kbd>z</kbd> does the same thing for readers who never reach for the mouse.

Exit is never keyboard-only either. Because Zen also sets the maximized class, the corner
exit chip appears and returns the reader to normal — necessary, since the tab row (and both
its buttons) is hidden while focused.

## Error and boundary cases

| Case | Intended behavior |
| --- | --- |
| <kbd>z</kbd> while typing in the editor or a form field | Inserts the letter; the mode is untouched. |
| <kbd>Ctrl/⌘/Alt</kbd> + <kbd>z</kbd> | Ignored, so undo and OS shortcuts still work. |
| <kbd>z</kbd> from full screen | Deepens into Zen. |
| <kbd>f</kbd> or <kbd>z</kbd> from Zen | Returns straight to normal. |
| <kbd>Esc</kbd> already handled by another widget | Yields — the editor's search closes first, as before. |
| Reader had already collapsed a panel | Zen changes nothing for them, and their choice still holds on exit. |
| A tab with no secondary panel | Zen simply hides the chrome; there is nothing else to step aside. |
| Reload while in Zen | Returns to normal — Zen is a posture, not a persisted layout. |
| Focus inside a panel Zen hides | Blurred on entry, so an invisible control cannot be activated by Enter. |

## Integration

- **Client:** `fullscreen.ts` owns the mode, its keys, the focus release, and the button
  state; `zen-button.ts` builds the control and `maximize-controls.ts` installs it beside
  maximize; `styles.css` carries the `body.zen` rules; `help.ts` documents the shortcut per
  tab. `app.ts` passes `toggleZen` through and hides both controls in the empty state.
- **Core:** unchanged. Zen is presentation only.

## Testability

- **Unit** (`fullscreen.test.ts`, `maximize-controls.test.ts`): mode transitions across
  normal / full screen / Zen, both keys and Escape, modifier and text-entry guards, button
  labelling (including that the Zen button reads engaged only in Zen), the control's glyph
  and ordering, and that focus is released from a hidden region but left alone elsewhere.
- **End-to-end** (`fullscreen.spec.ts`, `config.spec.ts`, `semantic.spec.ts`): Zen on each
  of the four surfaces hides the right panel and keeps the right content; the tab-row button
  enters Zen and the corner chip leaves it; a reader's own collapse choice survives a Zen
  round trip.
- **End-to-end, served** (`live.spec.ts`): Zen hides the Explorer on a report that has one —
  the case a static fixture cannot cover, since the sidebar only exists when served.

## Follow-ups

- **A centered editor column** in Zen on Source and Config, as VS Code offers, if the full
  width reads too wide on a large display.
- **A discoverability hint** the first time a reader enters Zen, if the button and footer
  help prove too quiet a home for the shortcut.
