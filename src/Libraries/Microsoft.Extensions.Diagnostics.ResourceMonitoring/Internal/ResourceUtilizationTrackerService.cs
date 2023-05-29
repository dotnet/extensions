// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// The implementation of <see cref="IResourceMonitor"/> that computes average resource utilization over a configured period of time.
/// </summary>
/// <remarks>
/// The class also acts as a hosted singleton, intended to be used to manage the
/// background process of periodically inspecting and monitoring the utilization
/// of an enclosing system.
/// </remarks>
internal sealed class ResourceUtilizationTrackerService : BackgroundService, IResourceMonitor
{
    /// <summary>
    /// The data source.
    /// </summary>
    private readonly ISnapshotProvider _provider;

    /// <summary>
    /// The publishers to use with the data we are tracking.
    /// </summary>
    private readonly IResourceUtilizationPublisher[] _publishers;

    /// <summary>
    /// Logger to be used in this class.
    /// </summary>
    private readonly ILogger<ResourceUtilizationTrackerService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Circular buffer for storing samples.
    /// </summary>
    private readonly CircularBuffer<ResourceUtilizationSnapshot> _snapshotsStore;

    private readonly TimeSpan _samplingInterval;

    private readonly TimeSpan _calculationPeriod;

    private readonly TimeSpan _collectionWindow;

    private readonly CancellationTokenSource _stoppingTokenSource = new();

    public ResourceUtilizationTrackerService(
        ISnapshotProvider provider,
        ILogger<ResourceUtilizationTrackerService> logger,
        IOptions<ResourceMonitoringOptions> options,
        IEnumerable<IResourceUtilizationPublisher> publishers)
        : this(provider, logger, options, publishers, TimeProvider.System)
    {
    }

    internal ResourceUtilizationTrackerService(
        ISnapshotProvider provider,
        ILogger<ResourceUtilizationTrackerService> logger,
        IOptions<ResourceMonitoringOptions> options,
        IEnumerable<IResourceUtilizationPublisher> publishers,
        TimeProvider timeProvider)
    {
        _provider = provider;
        _logger = logger;
        _timeProvider = timeProvider;
        var optionsValue = Throw.IfMemberNull(options, options.Value);
        _calculationPeriod = optionsValue.CalculationPeriod;
        _samplingInterval = optionsValue.SamplingInterval;
        _collectionWindow = optionsValue.CollectionWindow;

        _publishers = publishers.ToArray();

        var bufferSize = (int)(_collectionWindow.TotalMilliseconds / _samplingInterval.TotalMilliseconds);

        var firstSnapshot = _provider.GetSnapshot();

        _snapshotsStore = new CircularBuffer<ResourceUtilizationSnapshot>(bufferSize + 1, firstSnapshot);
    }

    /// <summary>
    /// Dispose the tracker.
    /// </summary>
    public override void Dispose()
    {
        _stoppingTokenSource.Dispose();
        base.Dispose();
    }

    /// <inheritdoc />
    public Utilization GetUtilization(TimeSpan window)
    {
        _ = Throw.IfLessThanOrEqual(window.Ticks, 0);
        _ = Throw.IfGreaterThan(window.Ticks, _collectionWindow.Ticks);

        var samplesToRead = (int)(window.Ticks / _samplingInterval.Ticks) + 1;
        (ResourceUtilizationSnapshot first, ResourceUtilizationSnapshot last) t;

        lock (_snapshotsStore)
        {
            t = _snapshotsStore.GetFirstAndLastFromWindow(samplesToRead);
        }

        return Calculator.CalculateUtilization(t.first, t.last, _provider.Resources);
    }

    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop the execution.
#if NET8_0_OR_GREATER
        await _stoppingTokenSource.CancelAsync().ConfigureAwait(false);
#else
        _stoppingTokenSource.Cancel();
#endif
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentionally Consume All. Allow no escapes.")]
    internal async Task PublishUtilizationAsync(CancellationToken cancellationToken)
    {
        var u = GetUtilization(_calculationPeriod);
        foreach (var publisher in _publishers)
        {
            try
            {
                await publisher.PublishAsync(u, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // By Design: Swallow the exception, as they're non-actionable in this code path.
                // Prioritize app reliability over error visibility
                Log.HandlePublishUtilizationException(_logger, e, publisher.GetType().FullName!);
            }
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentionally Consume All. Allow no escapes.")]
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stoppingTokenSource.Token);
        var linkedTokenSourceToken = linkedTokenSource.Token;

        while (!linkedTokenSourceToken.IsCancellationRequested)
        {
            await _timeProvider.Delay(_samplingInterval, linkedTokenSourceToken).ConfigureAwait(false);

            try
            {
                _snapshotsStore.Add(_provider.GetSnapshot());
            }
            catch (Exception e)
            {
                // By Design: Swallow the exception, as they're non-actionable in this code path.
                // Prioritize app reliability over error visibility
                Log.HandledGatherStatisticsException(_logger, e);
            }

            await PublishUtilizationAsync(linkedTokenSource.Token).ConfigureAwait(false);
        }
    }
}
