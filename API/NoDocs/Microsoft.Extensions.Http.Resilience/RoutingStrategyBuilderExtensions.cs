// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

public static class RoutingStrategyBuilderExtensions
{
    public static IRoutingStrategyBuilder ConfigureOrderedGroups(this IRoutingStrategyBuilder builder, IConfigurationSection section);
    public static IRoutingStrategyBuilder ConfigureOrderedGroups(this IRoutingStrategyBuilder builder, Action<OrderedGroupsRoutingOptions> configure);
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IRoutingStrategyBuilder ConfigureOrderedGroups(this IRoutingStrategyBuilder builder, Action<OrderedGroupsRoutingOptions, IServiceProvider> configure);
    public static IRoutingStrategyBuilder ConfigureWeightedGroups(this IRoutingStrategyBuilder builder, IConfigurationSection section);
    public static IRoutingStrategyBuilder ConfigureWeightedGroups(this IRoutingStrategyBuilder builder, Action<WeightedGroupsRoutingOptions> configure);
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IRoutingStrategyBuilder ConfigureWeightedGroups(this IRoutingStrategyBuilder builder, Action<WeightedGroupsRoutingOptions, IServiceProvider> configure);
}
