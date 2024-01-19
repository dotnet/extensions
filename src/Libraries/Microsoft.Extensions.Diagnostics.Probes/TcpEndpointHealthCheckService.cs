// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Probes;

/// <summary>
/// Opens a TCP port if the service is healthy and closes it otherwise.
/// </summary>
internal sealed class TcpEndpointHealthCheckService : BackgroundService
{
    internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    private readonly ILogger<TcpEndpointHealthCheckService> _logger;
    private readonly HealthCheckService _healthCheckService;
    private readonly TcpEndpointOptions _options;
#pragma warning disable CA2213 // 'TcpEndpointHealthCheckService' contains field '_listener' that is of IDisposable type 'TcpListener'
    private readonly TcpListener _listener;
#pragma warning restore CA2213

    public TcpEndpointHealthCheckService(ILogger<TcpEndpointHealthCheckService> logger, HealthCheckService healthCheckService, TcpEndpointOptions options)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
        _options = options;

        _listener = new TcpListener(IPAddress.Any, _options.TcpPort);
    }

    internal async Task UpdateHealthStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync(_options.FilterChecks, cancellationToken).ConfigureAwait(false);
            if (report.Status == HealthStatus.Healthy)
            {
                if (!_listener.Server.IsBound)
                {
                    _listener.Start(_options.MaxPendingConnections);
                    _ = Task.Run(() => OpenTcpAsync(cancellationToken), cancellationToken);
                }
            }
            else
            {
                _listener.Stop();
            }
        }
        catch (SocketException ex)
        {
            _logger.SocketExceptionCaughtTcpEndpoint(ex);
        }
    }

    /// <summary>
    /// Executes the health checks in the <see cref="HealthCheckService"/> and opens the registered TCP port if the service is healthy and closes it otherwise.
    /// </summary>
    /// <param name="stoppingToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Task.</returns>
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdateHealthStatusAsync(stoppingToken).ConfigureAwait(false);
            await TimeProvider.Delay(_options.HealthAssessmentPeriod, stoppingToken).ConfigureAwait(false);
        }

        _listener.Stop();
    }

    [SuppressMessage("Blocker Bug", "S2190:Recursion should not be infinite", Justification = "runs in background")]
    private async Task OpenTcpAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
#if NET6_0_OR_GREATER
            using var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
#else
            using var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
#endif
        }
    }
}
