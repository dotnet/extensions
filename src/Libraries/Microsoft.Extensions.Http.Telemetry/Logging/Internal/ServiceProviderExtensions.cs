// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

internal static class ServiceProviderExtensions
{
    public static T GetRequiredKeyedServiceOrDefault<T>(this IServiceProvider serviceProvider, string? serviceKey)
        where T : notnull
    {
        return serviceKey is null
            ? serviceProvider.GetRequiredService<T>()
            : serviceProvider.GetRequiredKeyedService<T>(serviceKey);
    }
}
