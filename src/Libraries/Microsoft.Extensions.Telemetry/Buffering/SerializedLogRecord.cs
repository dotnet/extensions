// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Logging;

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
            serializedAttributes.Add(new KeyValuePair<string, string>(new string(attributes[i].Key), attributes[i].Value?.ToString() ?? string.Empty));
        }

        Attributes = serializedAttributes;

        // Serialize without StackTrace, which is already optionally available in the log attributes via the ExtendedLogger.
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
        Exception = JsonSerializer.Serialize(exception);
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        FormattedMessage = formattedMessage;
    }

    public IReadOnlyList<KeyValuePair<string, string>> Attributes { get; }
    public string? FormattedMessage { get; }
    public string? Exception { get; }

    public DateTimeOffset Timestamp { get; }

    public LogLevel LogLevel { get; }

    public EventId EventId { get; }
}
#endif
