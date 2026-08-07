---
title: The Last Train
author: DialogueDown examples
---

<!--
  A slice-of-life visual novel. It favors the presentation idioms: named custom
  actions for backgrounds, music, and sprites; styled inner thoughts; value
  queries for the player's name and the clock; a conditional block on how the
  evening ends; and a dynamic-weight random choice standing in for a character's
  uncertain feelings.

  Stable, recurring effects are PascalCase C# method calls a game team keeps
  stable — ShowBackground(...), PlayBgm(...), ShowSprite(...). Yuki is declared
  once; her shifting moods ride on ShowSprite calls, not on speaker tags (a
  speaker tag is global identity, not a per-line emotion). A one-off, freeform
  beat uses the default ("...") command instead.
-->

# The Platform

`ShowBackground("train platform", "rainy evening")` `PlayBgm("wistful_piano")`

The `"Time"` train is late, and the platform is empty except for the two of you.
You are `"PlayerName"`, and you have rehearsed this a dozen times without ever
saying it.

`ShowSprite("yuki", "shy")` Yuki @yuki #heroine: You did not have to wait with me,
you know. It is *pouring*.

*I know*, you think. *That was rather the point.*

- I like the rain. And the company.
    - `ShowSprite("yuki", "warm")` Yuki: `("Yuki hides a smile behind her sleeve")`
      ...Then I will not argue.
    - => [Under One Umbrella](#under-one-umbrella)
- Just making sure you get home safe.
    - `ShowSprite("yuki", "soft")` Yuki: That is very like you. Careful, and kind
      about it.
    - => [Under One Umbrella](#under-one-umbrella)

# Under One Umbrella

`ShowSprite("yuki", "close")` `PlayBgm("closer")`

Her umbrella is small, so you both lean toward its center, shoulders nearly
touching. The rain writes soft static over everything else.

<!-- The engine picks Yuki's fidget, weighted by how warm she feels right now. -->

- `Yuki.Warmth%` Yuki: ...Is this all right?
- `Yuki.Reserve%` Yuki: There — that keeps us both dry.
- `%` Yuki: The rain does not seem to be stopping.

Yuki: Can I ask you something ~~silly~~ real? If the train never came — if it just
*stayed* late forever — would that be so bad?

- Not if you were the one stranded with me.
    - `ShowSprite("yuki", "blushing")` Yuki: You— you cannot just *say* things like
      that.
    - => [The Confession](#the-confession)
- The train always comes eventually. That is the sad part.
    - `ShowSprite("yuki", "wistful")` Yuki: Mm. Eventually.
    - => [The Confession](#the-confession)
- Say nothing, and just listen to the rain with her.
    - *Some silences are answers too*, you think.
    - => [The Confession](#the-confession)

# The Confession

`ShowBackground("platform", "headlights approaching")` `PlayBgm("crescendo")`

Far down the line, a single headlight swells out of the dark. The last train,
finally, is coming.

`ShowSprite("yuki", "urgent")` Yuki: Before it gets here — `"PlayerName"`, I have
to—

- **Say it first.** Tell her the thing you rehearsed.
    - *No more waiting.* You say her name, and then the truth of it.
- Wait, and let her finish.
    - You hold still, and let her keep the words she came all this way with.

> `if` `Yuki.LovesPlayer?`
>
> `ShowSprite("yuki", "radiant")` `PlayBgm("theme_together")`
>
> Yuki: *Oh.* You too? All this time, you too? ![the two of you laughing under one umbrella #size=large #align=center](cg-together.png)
>
> The train sighs to a stop, doors open — and neither of you moves to board it.
>
> => [The end](#END)
>
> `else`
>
> `ShowSprite("yuki", "gentle")` `PlayBgm("bittersweet")`
>
> Yuki: I am glad you told me. I truly am. Let us start as friends — the real
> kind — and see where the next train takes us.
>
> => [The end](#END)
