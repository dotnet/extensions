// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Extensions used to register Request Checkpoint feature.
/// </summary>
public static class RequestCheckpointExtensions
{
    /// <summary>
    /// Adds all Request Checkpoint services.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddRequestCheckpoint(this IServiceCollection services);

    /// <summary>
    /// Registers Request Checkpoint related middlewares into the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.</param>
    /// <returns>The <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" /> instance.</returns>
    public static IApplicationBuilder UseRequestCheckpoint(this IApplicationBuilder builder);
}
