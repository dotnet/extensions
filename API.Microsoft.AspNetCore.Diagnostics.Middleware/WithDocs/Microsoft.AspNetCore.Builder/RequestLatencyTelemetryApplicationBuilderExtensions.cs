// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions to register the request latency telemetry middleware.
/// </summary>
public static class RequestLatencyTelemetryApplicationBuilderExtensions
{
    /// <summary>
    /// Registers middleware for request checkpointing.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IApplicationBuilder UseRequestCheckpoint(this IApplicationBuilder builder);

    /// <summary>
    /// Adds the request latency telemetry middleware to <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" /> request execution pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IApplicationBuilder UseRequestLatencyTelemetry(this IApplicationBuilder builder);
}
