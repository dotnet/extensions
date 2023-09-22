// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Testing;

/// <summary>
/// Collects log records sent to the fake logger.
/// </summary>
[DebuggerDisplay("Count = {Count}, LatestRecord = {LatestRecord}")]
[DebuggerTypeProxy(typeof(FakeLogCollectorDebugView))]
public class FakeLogCollector
{
    /// <summary>
    /// Gets the latest record that was created.
    /// </summary>
    /// <returns>
    /// The latest log record created.
    /// </returns>
    /// <exception cref="T:System.InvalidOperationException">No records have been captured.</exception>
    public FakeLogRecord LatestRecord { get; }

    /// <summary>
    /// Gets the number of log records captured by this collector.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Logging.Testing.FakeLogCollector" /> class.
    /// </summary>
    /// <param name="options">The options to control which log records to retain.</param>
    public FakeLogCollector(IOptions<FakeLogCollectorOptions> options);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Logging.Testing.FakeLogCollector" /> class.
    /// </summary>
    public FakeLogCollector();

    /// <summary>
    /// Creates a new instance of the <see cref="T:Microsoft.Extensions.Logging.Testing.FakeLogCollector" /> class.
    /// </summary>
    /// <param name="options">The options to control which log records to retain.</param>
    /// <returns>The collector.</returns>
    public static FakeLogCollector Create(FakeLogCollectorOptions options);

    /// <summary>
    /// Removes all accumulated log records from the collector.
    /// </summary>
    public void Clear();

    /// <summary>
    /// Gets the records that are held by the collector.
    /// </summary>
    /// <param name="clear"><see langword="true" /> to atomically clear the set of accumulated log records; otherwise, <see langword="false" />.</param>
    /// <returns>
    /// The list of records tracked to date by the collector.
    /// </returns>
    public IReadOnlyList<FakeLogRecord> GetSnapshot(bool clear = false);
}
