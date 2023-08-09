// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
[DebuggerDisplay("{_measurements.Count} measurements")]
public sealed class MetricCollector<T> : IDisposable where T : struct
{
    public Instrument? Instrument { get; }
    public CollectedMeasurement<T>? LastMeasurement { get; }
    public MetricCollector(Instrument<T> instrument, TimeProvider? timeProvider = null);
    public MetricCollector(ObservableInstrument<T> instrument, TimeProvider? timeProvider = null);
    public MetricCollector(object? meterScope, string meterName, string instrumentName, TimeProvider? timeProvider = null);
    public MetricCollector(Meter meter, string instrumentName, TimeProvider? timeProvider = null);
    public void Dispose();
    public void Clear();
    public IReadOnlyList<CollectedMeasurement<T>> GetMeasurementSnapshot(bool clear = false);
    public Task WaitForMeasurementsAsync(int minCount, CancellationToken cancellationToken = default(CancellationToken));
    public Task WaitForMeasurementsAsync(int minCount, TimeSpan timeout);
    public void RecordObservableInstruments();
}
