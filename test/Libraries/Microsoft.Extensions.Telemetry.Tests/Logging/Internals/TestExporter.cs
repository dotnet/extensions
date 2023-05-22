// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Microsoft.Extensions.Telemetry.Logging.Test.Internals;

internal class TestExporter : BaseExporter<LogRecord>
{
    internal LogRecord? FirstLogRecord { get; set; }
    internal KeyValuePair<string, object?>? FirstScope { get; set; }
    internal List<KeyValuePair<string, object?>>? FirstState { get; set; }

    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        foreach (var logRecord in batch)
        {
            FirstLogRecord = logRecord;
            FirstState = logRecord.StateValues is null ? null : new(logRecord.StateValues);
            logRecord.ForEachScope(ProcessScope, this);
        }

        return ExportResult.Success;
    }

    private static void ProcessScope(LogRecordScope scope, TestExporter exporter)
    {
        using var enumerator = scope.GetEnumerator();
        enumerator.MoveNext();
        exporter.FirstScope = enumerator.Current;
    }
}
