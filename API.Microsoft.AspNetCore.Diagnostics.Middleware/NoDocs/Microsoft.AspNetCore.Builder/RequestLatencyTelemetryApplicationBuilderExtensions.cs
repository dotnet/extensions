// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

namespace Microsoft.AspNetCore.Builder;

public static class RequestLatencyTelemetryApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRequestCheckpoint(this IApplicationBuilder builder);
    public static IApplicationBuilder UseRequestLatencyTelemetry(this IApplicationBuilder builder);
}
