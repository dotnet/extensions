// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.Http.Resilience.Routing.Internal.OrderedGroups;
using Microsoft.Extensions.Http.Resilience.Routing.Internal.WeightedGroups;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

/// <summary>
/// Extension for <see cref="IRoutingStrategyBuilder"/>.
/// </summary>
public static class RoutingStrategyBuilderExtensions
{
    /// <summary>
    /// Configures ordered groups routing using <see cref="OrderedGroupsRoutingOptions"/>.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="section">The section that the <see cref="OrderedGroupsRoutingOptions"/> will bind against.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OrderedGroupsRoutingOptions))]
    public static IRoutingStrategyBuilder ConfigureOrderedGroups(this IRoutingStrategyBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.Services.Configure<OrderedGroupsRoutingOptions>(builder.Name, section);

        return builder.ConfigureOrderedGroupsCore();
    }

    /// <summary>
    /// Configures ordered groups routing using <see cref="OrderedGroupsRoutingOptions"/>.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="configure">The callback that configures <see cref="OrderedGroupsRoutingOptions"/>.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    public static IRoutingStrategyBuilder ConfigureOrderedGroups(this IRoutingStrategyBuilder builder, Action<OrderedGroupsRoutingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.ConfigureOrderedGroups((options, _) => configure(options));
    }

    /// <summary>
    /// Configures ordered groups routing using <see cref="OrderedGroupsRoutingOptions"/>.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="configure">The callback that configures <see cref="OrderedGroupsRoutingOptions"/>.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    [Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
    public static IRoutingStrategyBuilder ConfigureOrderedGroups(this IRoutingStrategyBuilder builder, Action<OrderedGroupsRoutingOptions, IServiceProvider> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services
            .AddOptions<OrderedGroupsRoutingOptions>(builder.Name)
            .Configure(configure);

        return builder.ConfigureOrderedGroupsCore();
    }

    /// <summary>
    /// Configures weighted groups routing using <see cref="WeightedGroupsRoutingOptions"/>.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="section">The section that the <see cref="WeightedGroupsRoutingOptions"/> will bind against.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WeightedGroupsRoutingOptions))]
    public static IRoutingStrategyBuilder ConfigureWeightedGroups(this IRoutingStrategyBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.Services.Configure<WeightedGroupsRoutingOptions>(builder.Name, section);

        return builder.ConfigureWeightedGroupsCore();
    }

    /// <summary>
    /// Configures weighted groups routing using <see cref="WeightedGroupsRoutingOptions"/>.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="configure">The callback that configures <see cref="WeightedGroupsRoutingOptions"/>.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    public static IRoutingStrategyBuilder ConfigureWeightedGroups(this IRoutingStrategyBuilder builder, Action<WeightedGroupsRoutingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.ConfigureWeightedGroups((options, _) => configure(options));
    }

    /// <summary>
    /// Configures weighted groups routing using <see cref="WeightedGroupsRoutingOptions"/>.
    /// </summary>
    /// <param name="builder">The routing builder.</param>
    /// <param name="configure">The callback that configures <see cref="WeightedGroupsRoutingOptions"/>.</param>
    /// <returns>
    /// The same routing builder instance.
    /// </returns>
    [Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
    public static IRoutingStrategyBuilder ConfigureWeightedGroups(this IRoutingStrategyBuilder builder, Action<WeightedGroupsRoutingOptions, IServiceProvider> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services
            .AddOptions<WeightedGroupsRoutingOptions>(builder.Name)
            .Configure(configure);

        return builder.ConfigureWeightedGroupsCore();
    }

    internal static IRoutingStrategyBuilder ConfigureRoutingStrategy(this IRoutingStrategyBuilder builder, Func<IServiceProvider, Func<RequestRoutingStrategy>> factory)
    {
        _ = builder.Services
            .AddOptions<RequestRoutingStrategyOptions>(builder.Name)
            .Configure<IServiceProvider>((options, provider) => options.RoutingStrategyProvider = factory(provider));

        return builder;
    }

    private static IRoutingStrategyBuilder ConfigureOrderedGroupsCore(this IRoutingStrategyBuilder builder)
    {
        return builder.ConfigureRoutingStrategy(serviceProvider =>
        {
            var optionsCache = new NamedOptionsCache<OrderedGroupsRoutingOptions>(builder.Name, serviceProvider.GetRequiredService<IOptionsMonitor<OrderedGroupsRoutingOptions>>());
            var factory = new OrderedGroupsRoutingStrategyFactory(serviceProvider.GetRequiredService<Randomizer>(), optionsCache);
            return () => factory.Get();
        });
    }


    private static IRoutingStrategyBuilder ConfigureWeightedGroupsCore(this IRoutingStrategyBuilder builder)
    {
        return builder.ConfigureRoutingStrategy(serviceProvider =>
        {
            var optionsCache = new NamedOptionsCache<WeightedGroupsRoutingOptions>(builder.Name, serviceProvider.GetRequiredService<IOptionsMonitor<WeightedGroupsRoutingOptions>>());
            var factory = new WeightedGroupsRoutingStrategyFactory(serviceProvider.GetRequiredService<Randomizer>(), optionsCache);
            return () => factory.Get();
        });
    }
}
