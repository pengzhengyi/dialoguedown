using Microsoft.AspNetCore.ResponseCompression;

namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// Response compression for the loopback server. The report's client bundle and each
/// page's payload are large, and gzip cuts their transfer roughly threefold. Only the
/// framework's default compressible MIME types are enabled, which excludes
/// <c>text/event-stream</c>, so the hot-reload SSE stream is never buffered by the
/// compressor.
/// </summary>
/// <remarks>
/// gzip is registered ahead of brotli: on this payload .NET's brotli is no smaller than
/// gzip yet several times slower to encode. The level stays at the framework default
/// (Fastest), so compression adds only a few ms per page load. Brotli stays registered
/// so a brotli-only client is still served compressed.
/// </remarks>
internal static class LoopbackCompression
{
    /// <summary>Registers gzip + brotli response-compression services (gzip preferred).</summary>
    public static void AddLoopbackCompression(this WebApplicationBuilder builder)
    {
        builder.Services.AddResponseCompression(options =>
        {
            options.Providers.Add<GzipCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
        });
    }
}

