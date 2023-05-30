// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Telemetry;

public static class RequestCheckpointExtensions
{
    public static IServiceCollection AddRequestCheckpoint(this IServiceCollection services);
    public static IApplicationBuilder UseRequestCheckpoint(this IApplicationBuilder builder);
}
