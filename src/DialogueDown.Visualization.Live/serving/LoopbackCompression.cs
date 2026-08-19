using Microsoft.AspNetCore.ResponseCompression;

namespace DialogueDown.Visualization.Live.Serving;

/// <summary>
/// Response compression for the loopback servers. The report page is a large
/// self-contained HTML document (hundreds of KB with the inlined bundle), so gzip
/// cuts its transfer roughly threefold — worthwhile whenever the report is viewed
/// over a LAN or VPN rather than pure loopback. Only the framework's default
/// compressible MIME types are enabled, which deliberately excludes
/// <c>text/event-stream</c>, so the hot-reload SSE stream is never buffered by the
/// compressor.
/// </summary>
/// <remarks>
/// gzip is registered ahead of brotli on purpose: measured against this payload,
/// .NET's brotli is no smaller than gzip yet several times slower to encode, and the
/// level is left at the framework default (Fastest) so compression adds only a few ms
/// per page load on the loopback-primary path. Brotli stays registered so a
/// brotli-only client is still served compressed.
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

