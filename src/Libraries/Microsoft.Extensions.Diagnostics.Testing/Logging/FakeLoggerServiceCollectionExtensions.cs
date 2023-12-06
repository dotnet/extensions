// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring fake logging, used in unit tests.
/// </summary>
public static class FakeLoggerServiceCollectionExtensions
{
    /// <summary>
    /// Configures fake logging.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="section">Configuration section that contains <see cref="FakeLogCollectorOptions"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddFakeLogging(this IServiceCollection services, IConfigurationSection section)
        => services.AddLogging(x => x.AddFakeLogging(section));

    /// <summary>
    /// Configures fake logging.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Logging configuration options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddFakeLogging(this IServiceCollection services, Action<FakeLogCollectorOptions> configure)
        => services.AddLogging(x => x.AddFakeLogging(configure));

    /// <summary>
    /// Configures fake logging with default options.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddFakeLogging(this IServiceCollection services)
        => services.AddLogging(builder => builder.AddFakeLogging());
}
