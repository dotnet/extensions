// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class RequestHeadersEnricherServiceCollectionExtensions
{
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services);
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, Action<RequestHeadersLogEnricherOptions> configure);
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, IConfigurationSection section);
}
