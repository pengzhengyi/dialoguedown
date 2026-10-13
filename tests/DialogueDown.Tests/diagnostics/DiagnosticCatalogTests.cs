using DialogueDown.Diagnostics;

namespace DialogueDown.Tests.Diagnostics;

public sealed class DiagnosticCatalogTests
{
    [Fact]
    public void EveryDescriptorHasAUniqueCode()
    {
        var codes = DiagnosticCatalogReflection.Descriptors().Select(descriptor => descriptor.Code).ToList();

        Assert.NotEmpty(codes);
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void All_ListsEveryDescriptorOnce()
    {
        // All is listed by hand; this keeps it in step with the descriptors declared above it.
        var reflected = DiagnosticCatalogReflection.Descriptors().Select(descriptor => descriptor.Code);

        Assert.Equal(reflected.Order(), DiagnosticCatalog.All.Select(descriptor => descriptor.Code).Order());
    }
}
