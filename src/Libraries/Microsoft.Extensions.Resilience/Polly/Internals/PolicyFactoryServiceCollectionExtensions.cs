// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Extension class for the Service Collection DI container.
/// </summary>
internal static class PolicyFactoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers to the <see cref="IServiceCollection" /> DI container a singleton policy pipeline factory <see cref="IPolicyFactory"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
    /// <param name="services">The DI container.</param>
    /// <returns>The input <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> cannot be null.</exception>
    public static IServiceCollection AddPolicyFactory<TResult>(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        _ = services.AddOptions<FailureEventMetricsOptions<TResult>>();
        _ = services.AddExceptionSummarizer();
        _ = services.RegisterMetering();

        services.TryAddTransient<IPolicyFactory, PolicyFactory>();
        services.TryAddTransient<IPolicyMetering, PolicyMetering>();

        return services;
    }
}
