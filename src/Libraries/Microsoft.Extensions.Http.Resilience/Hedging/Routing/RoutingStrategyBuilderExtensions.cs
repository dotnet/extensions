// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Internal.Routing;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
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

        _ = builder.Services.AddPooled<OrderedGroupsRoutingStrategy>();

        return builder.ConfigureRoutingStrategy<OrderedGroupsRoutingStrategyFactory, OrderedGroupsRoutingOptions, OrderedGroupsRoutingOptionsValidator>(options => options.Bind(section));
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
    [Experimental]
    public static IRoutingStrategyBuilder ConfigureOrderedGroups(this IRoutingStrategyBuilder builder, Action<OrderedGroupsRoutingOptions, IServiceProvider> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.AddPooled<OrderedGroupsRoutingStrategy>();

        return builder.ConfigureRoutingStrategy<OrderedGroupsRoutingStrategyFactory, OrderedGroupsRoutingOptions, OrderedGroupsRoutingOptionsValidator>(options => options.Configure(configure));
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

        _ = builder.Services.AddPooled<WeightedGroupsRoutingStrategy>();

        return builder.ConfigureRoutingStrategy<WeightedGroupsRoutingStrategyFactory, WeightedGroupsRoutingOptions, WeightedGroupsRoutingOptionsValidator>(options => options.Bind(section));
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
    [Experimental]
    public static IRoutingStrategyBuilder ConfigureWeightedGroups(this IRoutingStrategyBuilder builder, Action<WeightedGroupsRoutingOptions, IServiceProvider> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.AddPooled<WeightedGroupsRoutingStrategy>();

        return builder.ConfigureRoutingStrategy<WeightedGroupsRoutingStrategyFactory, WeightedGroupsRoutingOptions, WeightedGroupsRoutingOptionsValidator>(options => options.Configure(configure));
    }

    internal static IRequestRoutingStrategyFactory GetRoutingFactory(this IServiceProvider serviceProvider, string routingName)
    {
        return serviceProvider.GetRequiredService<INamedServiceProvider<IRequestRoutingStrategyFactory>>().GetRequiredService(routingName);
    }

    internal static IRoutingStrategyBuilder ConfigureRoutingStrategy<TRoutingStrategyFactory, TRoutingStrategyOptions, TRoutingStrategyOptionsValidator>(
        this IRoutingStrategyBuilder builder,
        Action<OptionsBuilder<TRoutingStrategyOptions>> configure)
        where TRoutingStrategyFactory : class, IRequestRoutingStrategyFactory
        where TRoutingStrategyOptions : class
        where TRoutingStrategyOptionsValidator : class, IValidateOptions<TRoutingStrategyOptions>
    {
        builder.Services.TryAddSingleton<IRandomizer, Randomizer>();

        var optionsBuilder = builder.Services.AddValidatedOptions<TRoutingStrategyOptions, TRoutingStrategyOptionsValidator>(builder.Name);
        configure(optionsBuilder);

        return builder.ConfigureRoutingStrategy(serviceProvider =>
        {
            return (IRequestRoutingStrategyFactory)ActivatorUtilities.CreateInstance(serviceProvider, typeof(TRoutingStrategyFactory), builder.Name);
        });
    }

    internal static IRoutingStrategyBuilder ConfigureRoutingStrategy(this IRoutingStrategyBuilder builder, Func<IServiceProvider, IRequestRoutingStrategyFactory> factory)
    {
        builder.Services.TryAddSingleton<IRandomizer, Randomizer>();

        _ = builder.Services.AddNamedSingleton(builder.Name, factory);

        return builder;
    }
}
