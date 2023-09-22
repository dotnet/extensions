// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience.FaultInjection;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

public static class HttpClientFaultInjectionExtensions
{
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services);
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services, IConfiguration section);
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services, Action<HttpFaultInjectionOptionsBuilder> configure);
    public static IHttpClientBuilder AddFaultInjectionPolicyHandler(this IHttpClientBuilder httpClientBuilder, string chaosPolicyOptionsGroupName);
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder httpClientBuilder, Action<FaultPolicyWeightAssignmentsOptions> weightAssignmentsConfig);
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder httpClientBuilder, IConfigurationSection weightAssignmentsConfigSection);
}
