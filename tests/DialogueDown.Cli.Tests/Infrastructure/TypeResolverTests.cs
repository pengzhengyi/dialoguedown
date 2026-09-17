using DialogueDown.Cli.Infrastructure;

namespace DialogueDown.Cli.Tests.Infrastructure;

public sealed class TypeResolverTests
{
    [Fact]
    public void Resolve_ANullType_IsNull()
    {
        using var resolver = new TypeResolver(new StubProvider());

        Assert.Null(resolver.Resolve(null));
    }

    [Fact]
    public void Resolve_ATypeTheProviderHolds_ReturnsWhatItHolds()
    {
        var service = new object();
        using var resolver = new TypeResolver(new StubProvider(service));

        Assert.Same(service, resolver.Resolve(typeof(object)));
    }

    [Fact]
    public void Dispose_AProviderThatCanBeDisposed_DisposesIt()
    {
        var provider = new DisposableProvider();

        new TypeResolver(provider).Dispose();

        Assert.True(provider.Disposed);
    }

    [Fact]
    public void Dispose_AProviderWithNothingToRelease_LeavesItAlone()
    {
        var resolver = new TypeResolver(new StubProvider());

        resolver.Dispose();
    }

    private sealed class StubProvider(object? service = null) : IServiceProvider
    {
        public object? GetService(Type serviceType) => service;
    }

    private sealed class DisposableProvider : IServiceProvider, IDisposable
    {
        public bool Disposed { get; private set; }

        public object? GetService(Type serviceType) => null;

        public void Dispose() => Disposed = true;
    }
}
