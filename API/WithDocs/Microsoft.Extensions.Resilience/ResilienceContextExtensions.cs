// Assembly 'Microsoft.Extensions.Resilience'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Http.Telemetry;
using Polly;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// Extensions for <see cref="T:Polly.ResilienceContext" />.
/// </summary>
[Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class ResilienceContextExtensions
{
    /// <summary>
    /// Sets the <see cref="T:Microsoft.Extensions.Http.Telemetry.RequestMetadata" /> to the <see cref="T:Polly.ResilienceContext" />.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <param name="requestMetadata">The request metadata.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="context" /> or <paramref name="requestMetadata" /> is <see langword="null" />.</exception>
    public static void SetRequestMetadata(this ResilienceContext context, RequestMetadata requestMetadata);

    /// <summary>
    /// Gets the <see cref="T:Microsoft.Extensions.Http.Telemetry.RequestMetadata" /> from the <see cref="T:Polly.ResilienceContext" />.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <returns>The instance of <see cref="T:Microsoft.Extensions.Http.Telemetry.RequestMetadata" /> or <see langword="null" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public static RequestMetadata? GetRequestMetadata(this ResilienceContext context);
}
