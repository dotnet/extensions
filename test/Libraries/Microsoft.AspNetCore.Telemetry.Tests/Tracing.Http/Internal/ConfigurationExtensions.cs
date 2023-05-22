// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Telemetry.Test.Internal;

internal static class ConfigurationExtensions
{
    public static IServiceCollection TryConfigureRedaction(this IServiceCollection services, Action<IRedactionBuilder>? config)
    {
        if (config == null)
        {
            return services;
        }

        return services.AddRedaction(config);
    }

    public static IWebHostBuilder TryConfigureServices(this IWebHostBuilder builder, Action<IServiceCollection>? config)
    {
        if (config == null)
        {
            return builder;
        }

        return builder.ConfigureServices(config);
    }
}
