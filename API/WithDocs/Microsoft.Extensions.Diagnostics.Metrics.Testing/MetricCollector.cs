// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.Metrics.Testing;

/// <summary>
/// Collects the measurements published from an <see cref="T:System.Diagnostics.Metrics.Instrument`1" /> or <see cref="T:System.Diagnostics.Metrics.ObservableInstrument`1" />.
/// </summary>
/// <typeparam name="T">The type of metric data being recorded.</typeparam>
[DebuggerDisplay("{_measurements.Count} measurements")]
public sealed class MetricCollector<T> : IDisposable where T : struct
{
    /// <summary>
    /// Gets the <see cref="P:Microsoft.Extensions.Diagnostics.Metrics.Testing.MetricCollector`1.Instrument" /> that is being recorded.
    /// </summary>
    /// <remarks>
    /// This may be <see langword="null" /> until the instrument is published.
    /// </remarks>
    public Instrument? Instrument { get; }

    /// <summary>
    /// Gets the latest measurement collected, if any.
    /// </summary>
    public CollectedMeasurement<T>? LastMeasurement { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.Metrics.Testing.MetricCollector`1" /> class.
    /// </summary>
    /// <param name="instrument">The <see cref="T:System.Diagnostics.Metrics.Instrument`1" /> to record measurements from.</param>
    /// <param name="timeProvider">The time provider to use, or <see langword="null" /> to use the system time provider.</param>
    public MetricCollector(Instrument<T> instrument, TimeProvider? timeProvider = null);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.Metrics.Testing.MetricCollector`1" /> class.
    /// </summary>
    /// <param name="instrument">The <see cref="T:System.Diagnostics.Metrics.ObservableInstrument`1" /> to record measurements from.</param>
    /// <param name="timeProvider">The time provider to use, or <see langword="null" /> to use the system time provider.</param>
    public MetricCollector(ObservableInstrument<T> instrument, TimeProvider? timeProvider = null);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.Metrics.Testing.MetricCollector`1" /> class.
    /// </summary>
    /// <param name="meterScope">The scope of the meter that publishes the instrument to record.
    /// Take caution when using Meters in the global scope (scope == null). This interacts with
    /// static mutable data and tests doing this should not be run in parallel with each other.
    /// </param>
    /// <param name="meterName">The name of the meter that publishes the instrument to record.</param>
    /// <param name="instrumentName">The name of the instrument to record.</param>
    /// <param name="timeProvider">The time provider to use, or <see langword="null" /> to use the system time provider.</param>
    /// <remarks>
    /// Both the meter name and scope are used to identity the meter of interest.
    /// </remarks>
    public MetricCollector(object? meterScope, string meterName, string instrumentName, TimeProvider? timeProvider = null);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.Metrics.Testing.MetricCollector`1" /> class.
    /// </summary>
    /// <param name="meter">The meter that publishes the instrument to record.</param>
    /// <param name="instrumentName">The name of the instrument to record.</param>
    /// <param name="timeProvider">The time provider to use, or <see langword="null" /> to use the system time provider.</param>
    public MetricCollector(Meter meter, string instrumentName, TimeProvider? timeProvider = null);

    /// <summary>
    /// Disposes the <see cref="T:Microsoft.Extensions.Diagnostics.Metrics.Testing.MetricCollector`1" /> and stops recording measurements.
    /// </summary>
    public void Dispose();

    /// <summary>
    /// Removes all accumulated measurements from the collector.
    /// </summary>
    public void Clear();

    /// <summary>
    /// Gets a snapshot of measurements collected by this collector.
    /// </summary>
    /// <param name="clear">Setting this to <see langword="true" /> will atomically clear the set of accumulated measurements.</param>
    /// <returns>The measurements recorded by this collector, ordered by recording time.</returns>
    public IReadOnlyList<CollectedMeasurement<T>> GetMeasurementSnapshot(bool clear = false);

    /// <summary>
    /// Returns a task that completes when the collector has collected a minimum number of measurements.
    /// </summary>
    /// <param name="minCount">The minimum number of measurements to wait for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the collector has collected the requisite number of measurements.</returns>
    public Task WaitForMeasurementsAsync(int minCount, CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// Returns a task that completes when the collector has collected a minimum number of measurements.
    /// </summary>
    /// <param name="minCount">The minimum number of measurements to wait for.</param>
    /// <param name="timeout">How long to wait.</param>
    /// <returns>A task that completes when the collector has collected the requisite number of measurements.</returns>
    public Task WaitForMeasurementsAsync(int minCount, TimeSpan timeout);

    /// <summary>
    /// Scan all registered observable instruments.
    /// </summary>
    public void RecordObservableInstruments();
}
