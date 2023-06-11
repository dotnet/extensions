// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Provides extension methods for Fault-Injection library.
/// </summary>
public static class FaultInjectionExtensions
{
    private const string ChaosPolicyOptionsGroupName = "ChaosPolicyOptionsGroupName";

    /// <summary>
    /// Registers default implementations for <see cref="IFaultInjectionOptionsProvider"/> and <see cref="IChaosPolicyFactory"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> so that additional calls can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddFaultInjection(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services.AddFaultInjection(builder => builder.Configure());
    }

    /// <summary>
    /// Configures <see cref="FaultInjectionOptions"/> and registers default implementations for
    /// <see cref="IFaultInjectionOptionsProvider"/> and <see cref="IChaosPolicyFactory"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="section">
    /// The configuration section to bind to <see cref="FaultInjectionOptions"/>.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> so that additional calls can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddFaultInjection(this IServiceCollection services, IConfiguration section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return services.AddFaultInjection(
            builder => builder.Configure(section));
    }

    /// <summary>
    /// Calls the given action to configure <see cref="FaultInjectionOptions"/> and registers default implementations for
    /// <see cref="IFaultInjectionOptionsProvider"/> and <see cref="IChaosPolicyFactory"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configure">Function to configure <see cref="FaultInjectionOptions"/>.</param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> so that additional calls can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddFaultInjection(this IServiceCollection services, Action<FaultInjectionOptionsBuilder> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        var builder = new FaultInjectionOptionsBuilder(services);
        configure.Invoke(builder);

        _ = services.RegisterMetering();

        services.TryAddSingleton<IFaultInjectionOptionsProvider, FaultInjectionOptionsProvider>();
        services.TryAddSingleton<IExceptionRegistry, ExceptionRegistry>();
        services.TryAddSingleton<ICustomResultRegistry, CustomResultRegistry>();
        services.TryAddSingleton<IChaosPolicyFactory, ChaosPolicyFactory>();

        return services;
    }

    /// <summary>
    /// Associates the given <see cref="Context"/> instance to the given identifier name
    /// for an <see cref="ChaosPolicyOptionsGroup"/> registered at <see cref="IFaultInjectionOptionsProvider"/>.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <param name="groupName">The identifier name for an <see cref="ChaosPolicyOptionsGroup"/>.</param>
    /// <returns>
    /// The <see cref="Context"/> so that additional calls can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Any of the parameters are <see langword="null"/>.
    /// </exception>
    public static Context WithFaultInjection(this Context context, string groupName)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(groupName);

        context[ChaosPolicyOptionsGroupName] = groupName;

        return context;
    }

    /// <summary>
    /// Associates <paramref name="weightAssignments"/> to the calling <see cref="Context"/> instance, where <paramref name="weightAssignments"/>
    /// will be used to determine which <see cref="ChaosPolicyOptionsGroup"/> to use at each fault-injection run.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <param name="weightAssignments">The fault policy weight assignment.</param>
    /// <returns>
    /// The <see cref="Context"/> so that additional calls can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Any of the parameters are <see langword="null"/>.
    /// </exception>
    [Experimental]
    public static Context WithFaultInjection(this Context context, FaultPolicyWeightAssignmentsOptions weightAssignments)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(weightAssignments);

        context[ChaosPolicyOptionsGroupName] = weightAssignments.WeightAssignments
            .OrderBy(pair => pair.Value)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        return context;
    }

    /// <summary>
    /// Gets the name of the registered <see cref="ChaosPolicyOptionsGroup"/> from <see cref="Context"/>.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <returns>
    /// The <see cref="ChaosPolicyOptionsGroup"/> if registered; <see langword="null"/> if it isn't.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Any of the parameters are <see langword="null"/>.
    /// </exception>
    public static string? GetFaultInjectionGroupName(this Context context)
    {
        _ = Throw.IfNull(context);

        if (context.TryGetValue(ChaosPolicyOptionsGroupName, out var contextObj))
        {
            if (contextObj is string name)
            {
                return name;
            }
            else if (contextObj is Dictionary<string, double> weightAssignments)
            {
                return GetGroupNameFromWeightAssignments(weightAssignments);
            }
        }

        return null;
    }

    private static string? GetGroupNameFromWeightAssignments(Dictionary<string, double> weightAssignments)
    {
        var maxValue = WeightAssignmentHelper.GetWeightSum(weightAssignments);
        var randNum = WeightAssignmentHelper.GenerateRandom(maxValue);
        var accumulatedVal = 0.0;
        string result = null!;

        foreach (var entry in weightAssignments)
        {
            accumulatedVal += entry.Value;
            if (WeightAssignmentHelper.IsUnderMax(randNum, accumulatedVal))
            {
                result = entry.Key;
                break;
            }
        }

        return result;
    }
}
