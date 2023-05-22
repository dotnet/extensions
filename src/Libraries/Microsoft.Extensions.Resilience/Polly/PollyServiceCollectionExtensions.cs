// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// Extension class for the Service Collection DI container.
/// </summary>
public static class PollyServiceCollectionExtensions
{
    /// <summary>
    /// Configures the failure result dimensions.
    /// </summary>
    /// <typeparam name="TResult">The type of the policy result.</typeparam>
    /// <param name="services">The services.</param>
    /// <param name="configure">The configure result dimensions.</param>
    /// <returns>The input <paramref name="services"/>.</returns>
    public static IServiceCollection ConfigureFailureResultContext<TResult>(
       this IServiceCollection services,
       Func<TResult, FailureResultContext> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services.Configure<FailureEventMetricsOptions<TResult>>(option => option.GetContextFromResult = configure);
    }
}
