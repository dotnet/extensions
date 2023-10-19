// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Unit testing friendly configured host.
/// </summary>
public sealed class FakeHost : IHost
{
    /// <summary>
    /// Gets the program's configured services.
    /// </summary>
    public IServiceProvider Services => _host.Services;
    internal TimeProvider TimeProvider = TimeProvider.System;
    private readonly IHost _host;
    private readonly FakeHostOptions _options;
    private bool _disposed;

    internal FakeHost(IHost host, FakeHostOptions options)
    {
        _host = host;
        _options = options;
    }

    /// <summary>
    /// Creates an instance of <see cref="IHostBuilder"/> to configure and build the host.
    /// </summary>
    /// <returns>An instance of <see cref="IHostBuilder"/>.</returns>
    public static IHostBuilder CreateBuilder() => new FakeHostBuilder(new FakeHostOptions());

    /// <summary>
    /// Creates an instance of <see cref="IHostBuilder"/> to configure and build the host.
    /// </summary>
    /// <param name="configure">The options to configure the <see cref="FakeHostOptions"/> instance.</param>
    /// <returns>An instance of <see cref="IHostBuilder"/>.</returns>
    public static IHostBuilder CreateBuilder(Action<FakeHostOptions> configure)
    {
        _ = Throw.IfNull(configure);

        var options = new FakeHostOptions();
        configure(options);
        return CreateBuilder(options);
    }

    /// <summary>
    /// Creates an instance of <see cref="IHostBuilder"/> to configure and build the host.
    /// </summary>
    /// <param name="options">An <see cref="FakeHostOptions"/> instance.</param>
    /// <returns>An instance of <see cref="IHostBuilder"/>.</returns>
    public static IHostBuilder CreateBuilder(FakeHostOptions options)
    {
        _ = Throw.IfNull(options);
        return new FakeHostBuilder(options);
    }

    /// <summary>
    /// Starts the program.
    /// </summary>
    /// <param name="cancellationToken">Used to abort program start.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the <see cref="IHost"/> starts.</returns>
    /// <remarks>If no cancellation token is given, a new one using <see cref="FakeHostOptions.StartUpTimeout"/> is used.</remarks>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        using var cancellationTokenSource = TimeProvider.CreateCancellationTokenSource(_options.StartUpTimeout);

        if (cancellationToken == default)
        {
            await _host.StartAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }
        else
        {
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                cancellationTokenSource.Token);
            await _host.StartAsync(linkedTokenSource.Token).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Attempts to gracefully stop the program.
    /// </summary>
    /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
    /// <returns>A <see cref="Task"/> that will be completed when the <see cref="IHost"/> stops.</returns>
    /// <remarks>If no cancellation token is given, a new one using <see cref="FakeHostOptions.StartUpTimeout"/> is used.</remarks>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        using var cancellationTokenSource = TimeProvider.CreateCancellationTokenSource(_options.ShutDownTimeout);

        if (cancellationToken == default)
        {
            await _host.StopAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }
        else
        {
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                cancellationTokenSource.Token);
            await _host.StopAsync(linkedTokenSource.Token).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Disposes the <see cref="IHost"/> instance.
    /// </summary>
    /// <remarks>Tries to gracefully shut down the host. Can be called multiple times.</remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        _host.Dispose();
    }
}
