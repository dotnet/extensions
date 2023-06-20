// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable R9A052

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Testing.Logging;

/// <summary>
/// Collects log records sent to the fake logger.
/// </summary>
[DebuggerDisplay("Count = {Count}, LatestRecord = {LatestRecord}")]
[DebuggerTypeProxy(typeof(FakeLogCollectorDebugView))]
public class FakeLogCollector
{
    private readonly List<FakeLogRecord> _records = new();
    private readonly FakeLogCollectorOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLogCollector"/> class.
    /// </summary>
    /// <param name="options">The options to control which log records to retain.</param>
    public FakeLogCollector(IOptions<FakeLogCollectorOptions> options)
    {
        _options = Throw.IfNullOrMemberNull(options, options?.Value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLogCollector"/> class.
    /// </summary>
    public FakeLogCollector()
    {
        _options = new FakeLogCollectorOptions();
    }

    /// <summary>
    /// Creates a new instance of the <see cref="FakeLogCollector"/> class.
    /// </summary>
    /// <param name="options">The options to control which log records to retain.</param>
    /// <returns>The collector.</returns>
    public static FakeLogCollector Create(FakeLogCollectorOptions options)
    {
        return new FakeLogCollector(Options.Options.Create(Throw.IfNull(options)));
    }

    /// <summary>
    /// Removes all accumulated log records from the collector.
    /// </summary>
    public void Clear()
    {
        lock (_records)
        {
            _records.Clear();
        }
    }

    /// <summary>
    /// Gets the records that are held by the collector.
    /// </summary>
    /// <param name="clear">Setting this to <see langword="true"/> will atomically clear the set of accumulated log records.</param>
    /// <returns>
    /// The list of records tracked to date by the collector.
    /// </returns>
    public IReadOnlyList<FakeLogRecord> GetSnapshot(bool clear = false)
    {
        lock (_records)
        {
            var records = _records.ToArray();
            if (clear)
            {
                _records.Clear();
            }

            return records;
        }
    }

    /// <summary>
    /// Gets the latest record that was created.
    /// </summary>
    /// <returns>
    /// The latest log record created.
    /// </returns>
    /// <exception cref="InvalidOperationException">No records have been captured.</exception>
    public FakeLogRecord LatestRecord
    {
        get
        {
            lock (_records)
            {
                if (_records.Count == 0)
                {
                    Throw.InvalidOperationException("No records logged.");
                }

                return _records[_records.Count - 1];
            }
        }
    }

    /// <summary>
    /// Gets the number of log records captured by this collector.
    /// </summary>
    public int Count => _records.Count;

    internal void AddRecord(FakeLogRecord record)
    {
        if (_options.FilteredLevels.Count > 0 && !_options.FilteredLevels.Contains(record.Level))
        {
            // level not being collected
            return;
        }

        if (_options.FilteredCategories.Count > 0)
        {
            if (record.Category == null || !_options.FilteredCategories.Contains(record.Category))
            {
                // no category specified, or not in the list of allowed categories
                return;
            }
        }

        if (!record.LevelEnabled && !_options.CollectRecordsForDisabledLogLevels)
        {
            // record is not enabled and we're not collecting disabled records
            return;
        }

        lock (_records)
        {
            _records.Add(record);
        }

        _options.OutputSink?.Invoke(_options.OutputFormatter(record));
    }

    internal TimeProvider TimeProvider => _options.TimeProvider;
}
