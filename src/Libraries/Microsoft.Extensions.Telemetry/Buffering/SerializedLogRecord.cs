// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal readonly struct SerializedLogRecord
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

        var serializedAttributes = new List<KeyValuePair<string, object?>>(attributes.Count);
#if NETFRAMEWORK
        for (int i = 0; i < attributes.Count; i++)
        {
            serializedAttributes.Add(new KeyValuePair<string, object?>(new string(attributes[i].Key.ToCharArray()), attributes[i].Value?.ToString() ?? string.Empty));
        }

        Exception = new string(exception?.Message.ToCharArray());
#else
        for (int i = 0; i < attributes.Count; i++)
        {
            serializedAttributes.Add(new KeyValuePair<string, object?>(new string(attributes[i].Key), attributes[i].Value?.ToString() ?? string.Empty));
        }

        Exception = new string(exception?.Message);
#endif
        Attributes = serializedAttributes;
        FormattedMessage = formattedMessage;
    }

    public IReadOnlyList<KeyValuePair<string, object?>> Attributes { get; }
    public string? FormattedMessage { get; }
    public string? Exception { get; }

    public DateTimeOffset Timestamp { get; }

    public LogLevel LogLevel { get; }

    public EventId EventId { get; }
}
