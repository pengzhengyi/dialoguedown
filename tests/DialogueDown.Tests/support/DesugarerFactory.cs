using DialogueDown.Script.Desugar;

namespace DialogueDown.Tests.Support;

/// <summary>Builds the desugar stage for tests in one place.</summary>
internal static class DesugarerFactory
{
    public static IScriptDesugarer ScriptDesugarer() => new ScriptDesugarer();
}
