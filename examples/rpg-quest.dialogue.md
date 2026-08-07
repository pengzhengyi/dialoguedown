---
title: The Ember Crown
author: DialogueDown examples
---

<!--
  A short fantasy quest. It leans on the game-state constructs: value queries for
  the hero's name and gold, boolean conditions on items and courage, weighted
  random choices to resolve a duel, and a conditional block on the final reward.

  Stable, recurring effects are named custom actions — PascalCase C# method calls
  a game team keeps stable, like PlayMusic(...), ShowBackground(...),
  GiveQuest(...), BuyShield(), and SpareDragon(). A one-off, freeform beat uses
  the default ("...") command instead.
-->

# The Tavern

`ShowBackground("tavern", "firelit")` `PlayMusic("tavern_lute")`

Rain hammers the shutters of the Salted Hart. You are `"HeroName"`, and your purse
holds `"Gold"` gold — enough for a bed, not for a legend.

Keeper @keeper #wary: You have the look of someone chasing trouble. The Ember Crown
was stolen up at Cinderfell. The old dragon Vharos sleeps on it now.

- Ask about the road to Cinderfell.
    - Keeper: North, past the burned mile. Take a torch — and take *care*.
    - => [The Mountain Road](#the-mountain-road)
- `Hero.HasSword?` Show the keeper your blade and set out at once. #bold
    - Keeper: Steel and nerve. Perhaps you *will* come back. `GiveQuest("EmberCrown")`
    - => [The Mountain Road](#the-mountain-road)
- Buy a shield for thirty gold before you go.
    - `BuyShield()` The keeper hands a battered round shield across the bar.
    - => [The Mountain Road](#the-mountain-road)

# The Mountain Road

`ShowBackground("mountain path", "dusk")` `PlayMusic("windy_ascent")`

The burned mile is exactly that — a mile of black stumps and cold ash. At its end,
a cave mouth breathes warm air into the dusk.

> `if` `Hero.IsBrave?`
>
> Your hands are steady. You have come too far to turn back for a draft of warm
> air and a bad reputation.
>
> => [The Dragon's Hall](#the-dragons-hall)
>
> `else`
>
> Your courage wavers at the threshold. You steady it the old way — one breath,
> then one step. `("the hero grips the torch tighter")`
>
> => [The Dragon's Hall](#the-dragons-hall)

# The Dragon's Hall

`ShowBackground("dragon hoard", "ember light")` `PlayMusic("battle")`

Gold drifts across the floor like dunes. Upon the tallest heap sits the Ember
Crown, and around it, vast and red, coils Vharos.

Vharos @dragon #ancient: A thief with a torch. *How the centuries repeat.* Draw
your little blade, then, and let us be quick.

- `Hero.HasSword?` Strike for the heart while the beast is coiled.
    - `Hero.Attack%` Your blade finds the gap between two scales, and Vharos
      *roars*. => [The Crown](#the-crown)
    - `Dragon.Fury%` A wing sweeps you off your feet before you land the blow.
      => [The Ashes](#the-ashes)
- `Hero.HasShield?` Raise the shield and wait for the fire to break.
    - `Hero.Guard%` The flames split around the round shield, and you dart inside
      the dragon's reach. => [The Crown](#the-crown)
    - `Dragon.Fury%` The heat curls the shield like paper. => [The Ashes](#the-ashes)
- Snatch the crown and run for the cave mouth. #reckless
    - `50%` You skid beneath a snapping jaw and out into the night, crown in
      hand. `SpareDragon()` => [The Crown](#the-crown)
    - `50%` The jaws close where you were a heartbeat too slow. => [The Ashes](#the-ashes)

# The Crown

`PlayMusic("victory")`

Vharos folds down into the hoard with a sound like a closing forge, and the hall
falls quiet. The Ember Crown is warm in your hands.

> `if` `Hero.SparedDragon?`
>
> The old dragon lives, and watches you with one banked-coal eye.
>
> Vharos @dragon #ancient: Take it, thief. A crown was always a poor bed.
>
> => [The end](#END)
>
> `else`
>
> You lift the crown from a silent hoard. The kingdom will call you many things;
> *hero* will be the kindest.
>
> => [The end](#END)

# The Ashes

`ShowBackground("cave mouth", "moonlit")` `PlayMusic("somber")`

You crawl back into the cold night with singed hair and empty hands. Vharos keeps
his crown — and his hall — for another hundred years.

Keeper @keeper #wary: You came *back*, at least. Not everyone does. Rest,
`"HeroName"`. The crown will keep. So will your nerve.

=> [The end](#END)
