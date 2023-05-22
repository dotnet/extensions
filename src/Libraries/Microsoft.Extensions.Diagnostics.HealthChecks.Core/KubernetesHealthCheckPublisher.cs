// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Opens a TCP port if the service is healthy and closes it otherwise.
/// </summary>
internal sealed class KubernetesHealthCheckPublisher : IHealthCheckPublisher
{
    private readonly TcpListener _server;
    private readonly int _maxLengthOfPendingConnectionsQueue;

    /// <summary>
    /// Initializes a new instance of the <see cref="KubernetesHealthCheckPublisher"/> class.
    /// </summary>
    /// <param name="options">Creation options.</param>
    public KubernetesHealthCheckPublisher(IOptions<KubernetesHealthCheckPublisherOptions> options)
    {
        var value = Throw.IfMemberNull(options, options.Value);

        _server = new TcpListener(IPAddress.Any, value.TcpPort);
        _maxLengthOfPendingConnectionsQueue = value.MaxPendingConnections;
    }

    /// <summary>
    /// Publishes the provided report.
    /// </summary>
    /// <param name="report">The <see cref="HealthReport"/>. The result of executing a set of health checks.</param>
    /// <param name="cancellationToken">Not used in the current implementation.</param>
    /// <returns>Task.CompletedTask.</returns>
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(report);

        if (report.Status == HealthStatus.Healthy)
        {
            if (!_server.Server.IsBound)
            {
                _server.Start(_maxLengthOfPendingConnectionsQueue);
                _ = Task.Run(() => TcpServerAsync(), CancellationToken.None);
            }
        }
        else
        {
            _server.Stop();
        }

        return Task.CompletedTask;
    }

    [SuppressMessage("Blocker Bug", "S2190:Recursion should not be infinite", Justification = "runs in background")]
    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = "runs in background")]
    private async Task TcpServerAsync()
    {
        while (true)
        {
            using var client = await _server.AcceptTcpClientAsync().ConfigureAwait(false);
        }
    }
}
