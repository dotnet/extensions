// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;

namespace System;

/// <summary>
/// Extensions for configuring fake logging, used in unit tests.
/// </summary>
public static class FakeLoggerServiceProviderExtensions
{
    /// <summary>
    /// Gets the object that collects log records sent to the fake logger.
    /// </summary>
    /// <param name="services">The service provider containing the logger.</param>
    /// <exception cref="InvalidOperationException">No collector exists in the provider.</exception>
    /// <returns>The collector that tracks records logged to fake loggers.</returns>
    public static FakeLogCollector GetFakeLogCollector(this IServiceProvider services)
        => services.GetService<FakeLogCollector>() ?? throw new InvalidOperationException("No fake log collector registered");
}
