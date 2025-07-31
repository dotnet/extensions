// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal static class SerializedLogRecordFactory
{
    private static readonly ObjectPool<List<KeyValuePair<string, object?>>> _attributesPool =
        PoolFactory.CreateListPool<KeyValuePair<string, object?>>();

    private static readonly int _serializedLogRecordSize = Unsafe.SizeOf<SerializedLogRecord>();

    public static SerializedLogRecord Create(
        LogLevel logLevel,
        EventId eventId,
        DateTimeOffset timestamp,
        IReadOnlyList<KeyValuePair<string, object?>> attributes,
        Exception? exception,
        string formattedMessage)
    {
        int sizeInBytes = _serializedLogRecordSize;
        List<KeyValuePair<string, object?>> serializedAttributes = _attributesPool.Get();
        for (int i = 0; i < attributes.Count; i++)
        {
            string key = attributes[i].Key;
            string value = attributes[i].Value?.ToString() ?? string.Empty;

            // deliberately not counting the size of the key,
            // as it is constant strings in the vast majority of cases

            sizeInBytes += CalculateStringSize(value);

            serializedAttributes.Add(new KeyValuePair<string, object?>(key, value));
        }

        string exceptionMessage = string.Empty;
        if (exception is not null)
        {
            exceptionMessage = exception.Message;
            sizeInBytes += CalculateStringSize(exceptionMessage);
        }

        sizeInBytes += CalculateStringSize(formattedMessage);

        return new SerializedLogRecord(
            logLevel,
            eventId,
            timestamp,
            serializedAttributes,
            exceptionMessage,
            formattedMessage,
            sizeInBytes);
    }

    public static void Return(SerializedLogRecord bufferedRecord)
    {
        _attributesPool.Return(bufferedRecord.Attributes);
    }

    private static int CalculateStringSize(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return 0;
        }

        // Base size: object overhead (16 bytes) + other stuff.
        const int BaseSize = 26;

        // Strings are aligned to 8-byte boundaries
        const int Alignment = 7;

        int charSize = str.Length * sizeof(char);
        return (BaseSize + charSize + Alignment) & ~Alignment;
    }
}
