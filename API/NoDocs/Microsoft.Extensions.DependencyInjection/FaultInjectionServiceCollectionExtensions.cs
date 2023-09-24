// Assembly 'Microsoft.Extensions.Resilience'

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Resilience.FaultInjection;
using Polly;

namespace Microsoft.Extensions.DependencyInjection;

public static class FaultInjectionServiceCollectionExtensions
{
    public static IServiceCollection AddFaultInjection(this IServiceCollection services);
    public static IServiceCollection AddFaultInjection(this IServiceCollection services, IConfiguration section);
    public static IServiceCollection AddFaultInjection(this IServiceCollection services, Action<FaultInjectionOptionsBuilder> configure);
    public static Context WithFaultInjection(this Context context, string groupName);
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static Context WithFaultInjection(this Context context, FaultPolicyWeightAssignmentsOptions weightAssignments);
    public static string? GetFaultInjectionGroupName(this Context context);
}
