using DialogueDown.Diagnostics;
using DialogueDown.Markdown;
using DialogueDown.Script.Ast;
using DialogueDown.Script.Transpiler.Builders;
using static DialogueDown.Tests.Support.DialogueAstAssert;

namespace DialogueDown.Tests.Support;

/// <summary>
/// Helpers for exercising and asserting <see cref="IInlinePolicy"/> outcomes, so a policy
/// test resolves an inline and inspects the result without repeating the diagnostic-sink setup.
/// </summary>
internal static class InlinePolicyAssert
{
    /// <summary>
    /// Resolves <paramref name="inline"/> through <paramref name="policy"/> with a throwaway
    /// sink, for tests that do not inspect the reported diagnostics.
    /// </summary>
    public static IReadOnlyList<InlineFragment> Resolve(IInlinePolicy policy, MarkdownInline inline) =>
        policy.Resolve(inline, new DiagnosticBag());

    /// <summary>
    /// Resolves <paramref name="inline"/>, returning the sink so a test can assert what the policy
    /// reported while recovering.
    /// </summary>
    public static IReadOnlyList<InlineFragment> Resolve(
        IInlinePolicy policy, MarkdownInline inline, out DiagnosticBag diagnostics)
    {
        diagnostics = new DiagnosticBag();
        return policy.Resolve(inline, diagnostics);
    }

    public static Text AssertResolvesToText(
        IInlinePolicy policy, MarkdownInline inline, string expected) =>
        AssertText(Assert.Single(Resolve(policy, inline)), expected);
}
