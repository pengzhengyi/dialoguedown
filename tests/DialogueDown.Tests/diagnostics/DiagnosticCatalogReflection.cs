using System.Reflection;
using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

// Shared enumeration of the catalog's descriptors. Reflecting over the public static
// DiagnosticDescriptor fields lets every reader — the uniqueness check and the documentation
// generator — cover the whole catalog without naming each entry by hand.
internal static class DiagnosticCatalogReflection
{
    public static IReadOnlyList<DiagnosticDescriptor> Descriptors() =>
        typeof(DiagnosticCatalog)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(DiagnosticDescriptor))
            .Select(field => Assert.IsType<DiagnosticDescriptor>(field.GetValue(null)))
            .ToList();
}
