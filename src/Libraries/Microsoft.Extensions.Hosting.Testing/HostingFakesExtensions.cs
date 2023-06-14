// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing.Internal;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Extension methods supporting host unit testing scenarios.
/// </summary>
[Experimental]
public static class HostingFakesExtensions
{
    /// <summary>
    /// Starts and immediately stops the service.
    /// </summary>
    /// <param name="service">The tested service.</param>
    /// <param name="cancellationToken">Cancellation token. See <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task StartAndStopAsync(this IHostedService service, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(service);

        try
        {
            await service.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await service.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the object that collects log records sent to the fake logger.
    /// </summary>
    /// <param name="host">An <see cref="IHost"/> instance.</param>
    /// <exception cref="InvalidOperationException">No collector exists in the provider.</exception>
    /// <returns>The collector that tracks records logged to fake loggers.</returns>
    public static FakeLogCollector GetFakeLogCollector(this IHost host)
    {
        _ = Throw.IfNull(host);
        return host.Services.GetFakeLogCollector();
    }

    /// <summary>
    /// Gets the object reporting all redactions performed.
    /// </summary>
    /// <param name="host">An <see cref="IHost"/> instance.</param>
    /// <exception cref="InvalidOperationException">No collector exists in the provider.</exception>
    /// <returns>The collector that tracks redactions performed on log messages.</returns>
    public static FakeRedactionCollector GetFakeRedactionCollector(this IHost host)
    {
        _ = Throw.IfNull(host);
        return host.Services.GetFakeRedactionCollector();
    }

    /// <summary>
    /// Adds an action invoked on each log message.
    /// </summary>
    /// <param name="builder">An <see cref="IHostBuilder"/> instance.</param>
    /// <param name="callback">The action to invoke on each log message.</param>
    /// <returns>The <see cref="IHostBuilder"/> instance.</returns>
    public static IHostBuilder AddFakeLoggingOutputSink(this IHostBuilder builder, Action<string> callback)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(callback);

        return builder.ConfigureServices(services => services.AddFakeLogging(logging =>
        {
            if (logging.OutputSink is null)
            {
                logging.OutputSink = callback;
            }
            else
            {
                var currentCallback = logging.OutputSink;
                logging.OutputSink = x =>
                {
                    currentCallback(x);
                    callback(x);
                };
            }
        }));
    }

    /// <summary>
    /// Exposes <see cref="IHostBuilder"/> for changes via a delegate.
    /// </summary>
    /// <param name="builder">An <see cref="IHostBuilder"/> instance.</param>
    /// <param name="configure">Configures the <see cref="IHostBuilder"/> instance.</param>
    /// <returns>The <see cref="IHostBuilder"/> instance.</returns>
    /// <remarks>Designed to ease host configuration in unit tests by defining common configuration methods.</remarks>
    [SuppressMessage("Minor Code Smell", "S3872:Parameter names should not duplicate the names of their methods",
        Justification = "We want to keep the parameter name for consistency.")]
    public static IHostBuilder Configure(this IHostBuilder builder, Action<IHostBuilder> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);
        configure(builder);
        return builder;
    }

    /// <summary>
    /// Adds configuration entries.
    /// </summary>
    /// <param name="builder">An <see cref="IHostBuilder"/> instance.</param>
    /// <param name="configurations">A list of key and value tuples that will be used as configuration entries.</param>
    /// <returns>The <see cref="IHostBuilder"/> instance.</returns>
    public static IHostBuilder ConfigureHostConfiguration(this IHostBuilder builder, params (string key, string value)[] configurations)
    {
        _ = Throw.IfNull(configurations);

        foreach ((var key, var value) in configurations)
        {
            _ = builder.ConfigureHostConfiguration(key, value);
        }

        return builder;
    }

    /// <summary>
    /// Adds a configuration value.
    /// </summary>
    /// <param name="builder">An <see cref="IHostBuilder"/> instance.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>The <see cref="IHostBuilder"/> instance.</returns>
    public static IHostBuilder ConfigureHostConfiguration(this IHostBuilder builder, string key, string value)
    {
        _ = Throw.IfNull(builder);
        return builder.ConfigureHostConfiguration(configBuilder => ConfigureConfiguration(configBuilder, key, value));
    }

    /// <summary>
    /// Adds configuration entries.
    /// </summary>
    /// <param name="builder">An <see cref="IHostBuilder"/> instance.</param>
    /// <param name="configurations">A list of key and value tuples that will be used as configuration entries.</param>
    /// <returns>The <see cref="IHostBuilder"/> instance.</returns>
    public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder builder, params (string key, string value)[] configurations)
    {
        _ = Throw.IfNull(configurations);

        foreach ((var key, var value) in configurations)
        {
            _ = builder.ConfigureAppConfiguration(key, value);
        }

        return builder;
    }

    /// <summary>
    /// Adds a configuration value.
    /// </summary>
    /// <param name="builder">An <see cref="IHostBuilder"/> instance.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>The <see cref="IHostBuilder"/> instance.</returns>
    public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder builder, string key, string value)
    {
        _ = Throw.IfNull(builder);
        return builder.ConfigureAppConfiguration((_, configBuilder) => ConfigureConfiguration(configBuilder, key, value));
    }

    private static void ConfigureConfiguration(IConfigurationBuilder builder, string key, string value)
    {
        if (builder.Sources.LastOrDefault() is FakeConfigurationSource source)
        {
            source.InitialData = source.InitialData!.Concat(new[] { new KeyValuePair<string, string?>(key, value) });
            return;
        }

        _ = builder.Add(new FakeConfigurationSource(new KeyValuePair<string, string?>(key, value)));
    }
}
