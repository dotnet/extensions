// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Resilience.FaultInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class HttpResilienceFaultInjectionHttpBuilderExtensions
{
    public static IHttpClientBuilder AddFaultInjectionPolicyHandler(this IHttpClientBuilder builder, string chaosPolicyOptionsGroupName);
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder builder, Action<FaultPolicyWeightAssignmentsOptions> weightAssignmentsConfig);
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder builder, IConfigurationSection weightAssignmentsConfigSection);
}
