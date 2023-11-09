// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Terminates its host after a timeout set in <see cref="FakeHostOptions.TimeToLive"/>.
/// </summary>
internal sealed partial class HostTerminatorService : BackgroundService
{
    internal bool DebuggerAttached = Debugger.IsAttached;
    internal TimeProvider TimeProvider = TimeProvider.System;
    private readonly IHost _host;
    private readonly FakeHostOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostTerminatorService"/> class.
    /// </summary>
    /// <param name="host">The <see cref="IHost"/> instance.</param>
    /// <param name="options">Options containing the time to live.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance.</param>
    public HostTerminatorService(IHost host, FakeHostOptions options, ILogger<HostTerminatorService> logger)
    {
        _host = host;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Waits till the time to live is up or till the service is stopped.
    /// </summary>
    /// <param name="stoppingToken">Triggered when <see cref="IHostedService.StopAsync(CancellationToken)"/> is called.</param>
    /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (DebuggerAttached)
        {
            LogDebuggerAttached();
            return;
        }

        await TimeProvider.Delay(_options.TimeToLive, stoppingToken).ConfigureAwait(false);

        using var timeoutTokenSource = TimeProvider.CreateCancellationTokenSource(_options.ShutDownTimeout);

        using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken,
            timeoutTokenSource.Token);

        LogTimeToLiveUp(_options.TimeToLive);
        await _host.StopAsync(combinedTokenSource.Token).ConfigureAwait(false);
        _host.Dispose();
    }

    [LoggerMessage(0, LogLevel.Warning, "FakeHostOptions.TimeToLive set to {TimeToLive} is up, disposing the host.")]
    private partial void LogTimeToLiveUp(TimeSpan timeToLive);

    [LoggerMessage(1, LogLevel.Information, "Debugger is attached. The host won't be automatically disposed.")]
    private partial void LogDebuggerAttached();
}
