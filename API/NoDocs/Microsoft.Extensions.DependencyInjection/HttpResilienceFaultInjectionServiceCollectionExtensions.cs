// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience.FaultInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class HttpResilienceFaultInjectionServiceCollectionExtensions
{
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services);
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services, IConfiguration section);
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services, Action<HttpFaultInjectionOptionsBuilder> configure);
}
