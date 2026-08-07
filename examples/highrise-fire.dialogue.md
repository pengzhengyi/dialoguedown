---
title: The Twelfth Floor
author: DialogueDown examples
---

<!--
  A high-rise fire drill written as dialogue. It exercises scenes, jumps, choices
  (tagged and conditional), a conditional block, line conditions, value queries,
  and game commands — while teaching real fire-service guidance: feel the door,
  take the stairs, stay low, and if you are trapped, seal the room and signal for
  help.

  Stable, recurring effects are named custom actions — PascalCase C# method calls
  a game team keeps stable, like PlaySound(...) and ShowBackground(...). A
  one-off, freeform beat uses the default ("...") command instead.
-->

# The Alarm

`ShowBackground("apartment", "night")` `PlaySound("fire_alarm")`

Narrator @narrator ##default: The smoke detector shrieks. You are on floor
`"Floor"`, and the hallway already smells of smoke.

Marshal @marshal #calm: Breathe — you have practiced this. First move: what do you
do?

1. => [Feel the hallway door before you open it](#the-door)
2. => [Call the elevator and hurry down](#the-elevator)
3. Grab the laptop and the photos first #panic
    - Marshal: Things can be replaced. **You** cannot. Leave them and move.
    - => [Feel the hallway door before you open it](#the-door)

# The Door

You reach for the handle. `("the resident feels the door with the back of a hand")`

> `if` `DoorIsHot?`
>
> The metal is *hot*. Fire is close on the other side.
>
> Marshal: Do not open it. Keep it shut — this room is your refuge now.
>
> => [Shelter in place](#shelter-in-place)
>
> `else`
>
> The door is cool, and no smoke seeps beneath it. It is safe to open, slowly.
>
> => [Head for the stairwell](#the-stairwell)

# The Stairwell

`ShowBackground("stairwell", "hazy")`

Smoke drifts along the ceiling in a slow grey river. The stairwell door waits just
ahead.

Marshal: Never the elevator in a fire — always the stairs. Pull each door shut
behind you; every one buys time.

- => [Crawl low and take the stairs down](#outside)
- `Resident.KnowsExits?` => [Use the second exit you scouted last week](#outside)
- Stand tall and run through the smoke #danger
    - Marshal: No — the breathable air is **low**. Get down and crawl.
    - => [Crawl low and take the stairs down](#outside)

# The Elevator

`PlaySound("elevator_button")`

You press the call button. The panel light stutters once — and goes dark.

Marshal: In a fire, an elevator can stall on the burning floor or fill with smoke.
This is the one door you must never choose.

Marshal: Back to your own door. Let us do this right.

=> [Feel the hallway door before you open it](#the-door)

# Shelter in Place

`ShowBackground("apartment", "sealed")`

You keep the hot door shut and back away. Help will come to you here.

Marshal: Seal the gaps. Wet towels along the bottom of the door hold the smoke
back.

- Pack wet towels into the door gap and over the vents.
- Throw the windows wide open #danger
    - Marshal: Not wide — a hard draft pulls smoke in. A hand's width at the top
      is enough.

Now call emergency services and give them your exact location: floor `"Floor"`,
the door that faces the street.

- `Resident.HasLight?` Signal at the glass with your flashlight.
- Wave a bright cloth from the window so the crews can find you.

=> [Rescued](#rescued)

# Outside

`ShowBackground("street", "flashing lights")` `PlaySound("sirens")`

Cold night air. You are out, and the muster point is across the road.

Marshal: The hardest rule of all now — **never go back inside**. Account for your
neighbors from here, and let the firefighters do the rest.

You: I made it. And next time, I will know exactly what to do.

=> [The end](#END)

# Rescued

A ladder rises to the window, and a gloved hand reaches through the smoke.

Marshal: A closed door, a sealed room, and a signal they could see — you did every
part of it right. *That* is how people come home.

=> [The end](#END)
