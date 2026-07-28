---
title: The Curious Traveler
author: DialogueDown demo
---

# The Crossroads

`("fade in")`

A weathered signpost marks where three roads meet.

Guide @guide #wise: Welcome, traveler. The road behind you is closed — you must
choose where to go next.

Alice: I have walked for days. Which way leads to the *market*?

Guide: The market lies east, past the old mill. But beware — the west road turns
**dangerous** after dark. `playSound("wind_howl")`

Guide: What will you do, `"playerName"`?

- => [Take the east road to the market](#the-market)
- => [Brave the west road](#the-dark-forest)
- `Alice.HasMap?` Slip onto the hidden trail through the hills.
- Ask the guide for advice first #cautious

# The Market

`("crossfade")`

Merchant @merchant: Fresh apples! Warm bread! Come, see my wares.

Alice: This place is *wonderful*.

`Alice.HasMap?` Alice: And I still have the old map — good.

If she still carries the old map, a hidden trail winds back through the hills.

`Alice.HasMap?` => [Brave the west road](#the-dark-forest)

=> [Leave town](#the-crossroads)

# The Dark Forest

The trees close in overhead, and something rustles in the shadows. #mysterious

- `60%` A fox darts across the path and vanishes.
- `40%` An owl watches you from a low branch.

Deeper in, the forest seems to weigh what it senses in you.

- `Alice.Courage%` Alice stands tall and presses on.
- `Alice.Fear%` Alice startles at every shifting shadow.
- `%` Alice walks on, uncertain.

Alice: Maybe this was a **mistake**.

- => [Turn back to the crossroads](#the-crossroads)
- => [Follow the moonlight ahead](#the-quiet-lake)

# The Quiet Lake

The trees give way to a still lake, silver under the moon. The long road is over.

Alice: I found my own way, in the end.

=> [The end](#END)
