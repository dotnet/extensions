// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System;
using Microsoft.AspNetCore.Diagnostics.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class RequestHeadersEnricherServiceCollectionExtensions
{
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services);
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, Action<RequestHeadersLogEnricherOptions> configure);
}
