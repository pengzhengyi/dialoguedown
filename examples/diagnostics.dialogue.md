---
title: "Broken on Purpose: a tour of errors and warnings"
author: DialogueDown examples
---

<!--
  This script is DELIBERATELY broken. It exists to show how the report surfaces
  diagnostics — errors and warnings — in the Diagnostics panel and inline on the
  source. Each mistake is labeled with the DLG code it triggers; the other
  examples show the same constructs used correctly.

  The mistakes here are all warnings before the semantic stage plus a few
  semantic-stage errors, so every pipeline stage still has something to show.
-->

# The Foyer

`ShowBackground("manor", "midnight")`

A door groans somewhere above you. Two ways lead deeper into the dark.

<!-- DLG3003 (warning): these static weights total 60%, not 100%. -->
- `30%` A cold draft tugs you toward the west wing.
- `30%` Candlelight gutters somewhere to the east.

<!-- DLG2009 (error): no scene owns this anchor. -->
=> [Down to the cellar](#the-cellar)

# The Hall

<!-- DLG1107 (warning): the speaker's name is styled, so the line has no speaker. -->
*Ghost*: You should not have come here.

<!-- DLG2007 (error): @caretaker is referenced but never declared with a name. -->
@caretaker: Pay the specter no mind. This way, quickly.

<!-- DLG1003 (warning): text after a jump on one line can never play. -->
=> [The Foyer](#the-foyer) and the candle went out.

# The Hall

<!-- DLG2001 (error): this second "# The Hall" slugs to the same anchor. -->
The corridor doubles back on itself, impossibly.

<!-- DLG3004 (warning): a one-option random choice is always chosen. -->
- `50%` The floorboards sigh under your weight.

=> [The end](#END)
