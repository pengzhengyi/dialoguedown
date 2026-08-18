using System.Text.Json.Serialization;

namespace DialogueDown.Playbook;

/// <summary>
/// One piece of what a line says. Speech is a list of these rather than a string, because a
/// host renders it: Godot as BBCode, the report as HTML, a terminal as ANSI.
/// </summary>
/// <remarks>
/// Fragments nest — styling wraps more fragments — so the encoding is recursive. Each is
/// tagged with a <c>kind</c>, and the union is closed to the kinds listed here.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = PlaybookJson.Discriminator)]
[JsonDerivedType(typeof(TextFragment), FragmentKinds.Text)]
[JsonDerivedType(typeof(StyledTextFragment), FragmentKinds.Styled)]
[JsonDerivedType(typeof(LinkFragment), FragmentKinds.Link)]
[JsonDerivedType(typeof(ImageFragment), FragmentKinds.Image)]
[JsonDerivedType(typeof(LineBreakFragment), FragmentKinds.Break)]
public abstract record SpeechFragment
{
    private protected SpeechFragment()
    {
    }
}
