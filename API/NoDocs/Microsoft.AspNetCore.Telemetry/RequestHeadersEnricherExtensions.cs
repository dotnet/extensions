// Assembly 'Microsoft.AspNetCore.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Telemetry;

public static class RequestHeadersEnricherExtensions
{
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services);
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, Action<RequestHeadersLogEnricherOptions> configure);
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, IConfigurationSection section);
}
