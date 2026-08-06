# Game state

How a script reads from and writes to your game. A **query** asks the game a
question; a **command** tells it to do something. Both are written as inline code
spans, so a script stays readable Markdown.

Part of the [script language specification](script-language.md).

## Table of contents

- [Queries](#queries)
- [Commands](#commands)
- [Quoting a key](#quoting-a-key)

The current `IGameSystem` interface exposes two integration points:

```csharp
public interface IGameSystem
{
    string Query(string query);

    void Execute(string command);
}
```

The DSL will compile query and command syntax into calls to that adapter.

## Queries

A query reads game state and inserts the returned value into speech.

```ebnf
Query = "`" , QuotedString , "`" ;
```

Adapter example:

```csharp
public sealed class GameSystem : IGameSystem
{
    public string Query(string query)
    {
        return query switch
        {
            "Alice.FavoriteColor" => "red",
            _ => string.Empty
        };
    }

    public void Execute(string command)
    {
    }
}
```

Script:

```markdown
Bob: What's your favorite color?

Alice: My favorite color is `"Alice.FavoriteColor"`.
```

Actual speech after query resolution:

```markdown
Bob: What's your favorite color?

Alice: My favorite color is red.
```

> [!TIP]
> A query can also drive a random choice's odds — see
> [Dynamic weights](structure-and-flow.md#dynamic-weights).

## Commands

A command changes game state through `IGameSystem.Execute`.

```ebnf
DefaultCommand = "`" , "(" , QuotedString , ")" , "`" ;
CustomCommand  = "`" , Identifier , "(" , [ Arguments ] , ")" , "`" ;
Command        = DefaultCommand | CustomCommand ;
```

Adapter example:

```csharp
public sealed class GameSystem : IGameSystem
{
    public string Query(string query)
    {
        return string.Empty;
    }

    public void Execute(string command)
    {
        switch (command)
        {
            case "JoinClub(\"Alice\", \"Kung Fu\")":
                JoinClub("Alice", "Kung Fu");
                return;
        }
    }

    private static void JoinClub(string characterName, string clubName)
    {
        // Update game state here.
    }
}
```

Default command:

```markdown
Bob: Of course. You can join. `("Alice joins Kung Fu")`

Alice: Thank you!
```

Custom command:

```markdown
Bob: Of course. You can join. `JoinClub("Alice", "Kung Fu")`

Alice: Thank you!
```

Silent command:

```markdown
Alice: Bob, do you have a minute?

Bob: Yes. What can I do for you?

Alice: I like Chinese martial arts. Can I join the Kung Fu Club?

Bob: Of course.

`JoinClub("Alice", "Kung Fu")`

Alice: Thank you!
```

Under the hood, a silent command is an **effect**, not speech: it compiles to a
command-only control line that has no speaker, so it is never attributed to a
character or the default speaker.

The compiler will emit a special node for each game-system call. The node shape
and runtime execution contract are outside this document's scope.

## Quoting a key

Every key so far is written in straight double quotes. The quotes mark where the
key begins and ends, so it can hold any characters — including spaces.

Two constructs put a **sigil** right after the key: a
[condition](structure-and-flow.md#conditional-jumps) ends it with `?`, and a
[dynamic weight](structure-and-flow.md#dynamic-weights) ends it with `%`. There the sigil already marks
where the key ends, so you may **drop the quotes** and write the key plainly:

```markdown
`IsAngry?` => [The guard blocks your way](#blocked)

- `Bob.Affection%` Bob: ...good to see you.
```

The unquoted key is everything before the sigil, with surrounding spaces trimmed —
so a natural phrase reads well, as in `` `Is Alice happy?` ``. Prefer this unquoted
form; it is the one this guide uses by default.

Add the quotes back only to **escape** — when a key must *end* in a literal `?` or
`%`, or contain a `"`:

```markdown
`"Rainy?"?` => [Wait out the storm](#the-inn)
```

Here the key is the literal `Rainy?`; the final `?` is the condition. A
[value read](#queries) has no sigil to mark its end, so it is **always quoted**.
