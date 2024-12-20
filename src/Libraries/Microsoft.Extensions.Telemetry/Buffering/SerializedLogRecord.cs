// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.ExceptionJsonConverter;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal readonly struct SerializedLogRecord : ISerializedLogRecord
{
    public SerializedLogRecord(
        LogLevel logLevel,
        EventId eventId,
        DateTimeOffset timestamp,
        IReadOnlyList<KeyValuePair<string, object?>> attributes,
        Exception? exception,
        string formattedMessage)
    {
        LogLevel = logLevel;
        EventId = eventId;
        Timestamp = timestamp;

        var serializedAttributes = new List<KeyValuePair<string, string>>(attributes.Count);
        for (int i = 0; i < attributes.Count; i++)
        {
#if NETFRAMEWORK
            serializedAttributes.Add(new KeyValuePair<string, string>(new string(attributes[i].Key.ToCharArray()), attributes[i].Value?.ToString() ?? string.Empty));
#else
            serializedAttributes.Add(new KeyValuePair<string, string>(new string(attributes[i].Key), attributes[i].Value?.ToString() ?? string.Empty));
#endif
        }

        Attributes = serializedAttributes;

        // Serialize without StackTrace, which is already optionally available in the log attributes via the ExtendedLogger.
        Exception = JsonSerializer.Serialize(exception, ExceptionJsonContext.Default.Exception);

        FormattedMessage = formattedMessage;
    }

    public IReadOnlyList<KeyValuePair<string, string>> Attributes { get; }
    public string? FormattedMessage { get; }
    public string? Exception { get; }

    public DateTimeOffset Timestamp { get; }

    public LogLevel LogLevel { get; }

    public EventId EventId { get; }
}
