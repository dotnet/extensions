// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Interface for a buffer sink.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public interface IBufferSink
{
    /// <summary>
    /// Forward the <paramref name="serializedRecords"/> to all currently registered loggers.
    /// </summary>
    /// <param name="serializedRecords">serialized log records.</param>
    /// <typeparam name="T">Type of the log records.</typeparam>
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    void LogRecords<T>(IEnumerable<T> serializedRecords)
        where T : ISerializedLogRecord;
}
#endif
