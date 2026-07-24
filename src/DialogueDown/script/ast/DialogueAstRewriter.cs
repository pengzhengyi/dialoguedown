namespace DialogueDown.Script.Ast;

/// <summary>
/// An immutable tree transformer over the Dialogue AST. By default it rebuilds every node
/// with its rewritten children — an identity clone — so a subclass overrides only the
/// hooks for the nodes it changes and never repeats the traversal. Every node kind that
/// holds children exposes a <c>protected virtual</c> hook, down to a fragment sequence
/// (<see cref="RewriteFragments"/>) and a tag (<see cref="RewriteTag"/>), so an override
/// reaches that kind wherever it appears in the tree. Modelled on Roslyn's
/// <c>CSharpSyntaxRewriter</c>.
/// </summary>
internal abstract class DialogueAstRewriter
{
    public ScriptDocument Rewrite(ScriptDocument document) =>
        document with { Body = document.Body.Select(RewriteBlock).ToList() };

    protected virtual ScriptBlock RewriteBlock(ScriptBlock block) => block switch
    {
        Line line => RewriteLine(line),
        Choices choices => RewriteChoices(choices),
        RandomChoices random => RewriteRandomChoices(random),
        SceneHeading heading => RewriteSceneHeading(heading),
        _ => throw new ArgumentOutOfRangeException(
            nameof(block), block.GetType().Name,
            $"Cannot rewrite a block of kind '{block.GetType().Name}' because it is not one "
            + "of the known block types."),
    };

    protected virtual Line RewriteLine(Line line) =>
        line with
        {
            Speaker = line.Speaker is null ? null : RewriteSpeaker(line.Speaker),
            Speech = RewriteFragments(line.Speech),
        };

    // Declarations carry tags, so they recurse; references and DefaultSpeaker are leaves.
    // A null speaker is handled by RewriteLine, so this hook only sees a present speaker.
    protected virtual Speaker RewriteSpeaker(Speaker speaker) => speaker switch
    {
        SpeakerDeclaration declaration => new SpeakerDeclaration(
            declaration.Name, declaration.Id, RewriteTags(declaration.Tags), declaration.Span),
        PartialSpeakerDeclaration partial => new PartialSpeakerDeclaration(
            partial.Id, RewriteTags(partial.Tags), partial.Span),
        _ => speaker,
    };

    protected virtual SceneHeading RewriteSceneHeading(SceneHeading heading) =>
        new(RewriteFragments(heading.Title), heading.Level, heading.Span);

    protected virtual Choices RewriteChoices(Choices choices) =>
        choices with { Options = choices.Options.Select(RewriteChoice).ToList() };

    protected virtual Choice RewriteChoice(Choice choice) =>
        choice with { Body = choice.Body.Select(RewriteBlock).ToList() };

    protected virtual RandomChoices RewriteRandomChoices(RandomChoices random) =>
        random with { Options = random.Options.Select(RewriteRandomOption).ToList() };

    protected virtual RandomOption RewriteRandomOption(RandomOption option) =>
        option with { Body = option.Body.Select(RewriteBlock).ToList() };

    protected virtual IReadOnlyList<InlineFragment> RewriteFragments(
        IReadOnlyList<InlineFragment> fragments) =>
        fragments.Select(RewriteFragment).ToList();

    // Containers recurse through RewriteFragments so an override there reaches every nested
    // list; a tag routes through RewriteTag; other leaves (Text, GameCall, LineBreak,
    // JumpIndicator, …) pass through.
    protected virtual InlineFragment RewriteFragment(InlineFragment fragment) => fragment switch
    {
        StyledText styled => new StyledText(
            styled.Style, RewriteFragments(styled.Children), styled.Span),
        Image image => new Image(image.Source, RewriteFragments(image.Alt), image.Span),
        Link link => new Link(link.Target, RewriteFragments(link.Label), link.Span),
        Jump jump => new Jump(jump.Target, RewriteFragments(jump.Label), jump.Span),
        Tag tag => RewriteTag(tag),
        _ => fragment,
    };

    // A tag is a leaf (name and value only), so the default is identity; the hook exists so
    // a pass can transform tags wherever they appear — in speech or in a speaker prefix.
    protected virtual Tag RewriteTag(Tag tag) => tag;

    private IReadOnlyList<Tag> RewriteTags(IReadOnlyList<Tag> tags) =>
        tags.Select(RewriteTag).ToList();
}
