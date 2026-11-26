using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticCatalogTests
{
    [Fact]
    public void EveryDescriptorHasAUniqueCode()
    {
        var duplicates = DiagnosticCatalog.All
            .GroupBy(descriptor => descriptor.Code)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        Assert.NotEmpty(DiagnosticCatalog.All);
        Assert.Empty(duplicates);
    }
}
