// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="T:Microsoft.Extensions.Http.Resilience.IRoutingStrategyBuilder" />.
/// </summary>
public static class RoutingStrategyBuilderExtensions
{
    /// <summary>
    /// Configures ordered groups routing using <see cref="T:Microsoft.Extensions.Http.Resilience.OrderedGroupsRoutingOptions" />.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="section">The section that the <see cref="T:Microsoft.Extensions.Http.Resilience.OrderedGroupsRoutingOptions" /> will bind against.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    public static IRoutingStrategyBuilder ConfigureOrderedGroups(this IRoutingStrategyBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Configures ordered groups routing using <see cref="T:Microsoft.Extensions.Http.Resilience.OrderedGroupsRoutingOptions" />.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="configure">The callback that configures <see cref="T:Microsoft.Extensions.Http.Resilience.OrderedGroupsRoutingOptions" />.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    public static IRoutingStrategyBuilder ConfigureOrderedGroups(this IRoutingStrategyBuilder builder, Action<OrderedGroupsRoutingOptions> configure);

    /// <summary>
    /// Configures ordered groups routing using <see cref="T:Microsoft.Extensions.Http.Resilience.OrderedGroupsRoutingOptions" />.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="configure">The callback that configures <see cref="T:Microsoft.Extensions.Http.Resilience.OrderedGroupsRoutingOptions" />.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IRoutingStrategyBuilder ConfigureOrderedGroups(this IRoutingStrategyBuilder builder, Action<OrderedGroupsRoutingOptions, IServiceProvider> configure);

    /// <summary>
    /// Configures weighted groups routing using <see cref="T:Microsoft.Extensions.Http.Resilience.WeightedGroupsRoutingOptions" />.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="section">The section that the <see cref="T:Microsoft.Extensions.Http.Resilience.WeightedGroupsRoutingOptions" /> will bind against.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    public static IRoutingStrategyBuilder ConfigureWeightedGroups(this IRoutingStrategyBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Configures weighted groups routing using <see cref="T:Microsoft.Extensions.Http.Resilience.WeightedGroupsRoutingOptions" />.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="configure">The callback that configures <see cref="T:Microsoft.Extensions.Http.Resilience.WeightedGroupsRoutingOptions" />.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    public static IRoutingStrategyBuilder ConfigureWeightedGroups(this IRoutingStrategyBuilder builder, Action<WeightedGroupsRoutingOptions> configure);

    /// <summary>
    /// Configures weighted groups routing using <see cref="T:Microsoft.Extensions.Http.Resilience.WeightedGroupsRoutingOptions" />.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="configure">The callback that configures <see cref="T:Microsoft.Extensions.Http.Resilience.WeightedGroupsRoutingOptions" />.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IRoutingStrategyBuilder ConfigureWeightedGroups(this IRoutingStrategyBuilder builder, Action<WeightedGroupsRoutingOptions, IServiceProvider> configure);
}
