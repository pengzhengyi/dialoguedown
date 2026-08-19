using Tomlyn.Syntax;

namespace DialogueDown.ConfigurationLoader.Toml;

/// <summary>
/// Resolves a TOML key's flat name, shared by the readers so they agree on what a key is called. A
/// quoted key equals its bare form (<c>"name"</c> and <c>name</c> are the same key, per TOML),
/// while a dotted key (<c>a.b</c>) keeps its full text so it never matches a structural key and is
/// treated as unknown rather than read as only its first segment.
/// </summary>
internal static class TomlKeys
{
    public static string Name(KeySyntax? key)
    {
        var resolved = key!;
        if (resolved.DotKeys is { ChildrenCount: > 0 })
        {
            return resolved.ToString()!.Trim();
        }

        return resolved.Key is BareKeySyntax bare
            ? bare.Key!.Text!
            : ((StringValueSyntax)resolved.Key!).Value!;
    }
}
