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
    private const string OriginalFormat = "{OriginalFormat}";

    private static readonly ObjectPool<List<KeyValuePair<string, object?>>> _attributesPool =
        PoolFactory.CreateListPool<KeyValuePair<string, object?>>();

    private static readonly int _serializedLogRecordSize = Unsafe.SizeOf<SerializedLogRecord>();

    public static SerializedLogRecord Create(
        LogLevel logLevel,
        EventId eventId,
        DateTimeOffset timestamp,
        IReadOnlyList<KeyValuePair<string, object?>> attributes,
        Exception? exception,
        string formattedMessage,
        IExternalScopeProvider? scopeProvider)
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

        if (scopeProvider is not null)
        {
            sizeInBytes += AddScopes(serializedAttributes, scopeProvider);
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

    /// <summary>
    /// Adds the name/value pairs of the current scopes to the attributes of a log record.
    /// </summary>
    /// <returns>The approximate size in bytes of the added values.</returns>
    private static int AddScopes(List<KeyValuePair<string, object?>> serializedAttributes, IExternalScopeProvider scopeProvider)
    {
        int count = serializedAttributes.Count;
        scopeProvider.ForEachScope(static (scope, attributes) =>
        {
            if (scope is IReadOnlyList<KeyValuePair<string, object?>> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    AddScopeItem(attributes, list[i]);
                }
            }
            else if (scope is IEnumerable<KeyValuePair<string, object?>> items)
            {
                foreach (KeyValuePair<string, object?> item in items)
                {
                    AddScopeItem(attributes, item);
                }
            }
        }, serializedAttributes);

        int sizeInBytes = 0;
        for (int i = count; i < serializedAttributes.Count; i++)
        {
            sizeInBytes += CalculateStringSize((string)serializedAttributes[i].Value!);
        }

        // "{OriginalFormat}" needs to stay the last attribute.
        if (count > 0 && serializedAttributes[count - 1].Key == OriginalFormat)
        {
            KeyValuePair<string, object?> originalFormat = serializedAttributes[count - 1];
            serializedAttributes.RemoveAt(count - 1);
            serializedAttributes.Add(originalFormat);
        }

        return sizeInBytes;
    }

    private static void AddScopeItem(List<KeyValuePair<string, object?>> attributes, KeyValuePair<string, object?> item)
    {
        // Skips the scope's {OriginalFormat} in favor of the log record's own {OriginalFormat}.
        if (item.Key != OriginalFormat)
        {
            attributes.Add(new KeyValuePair<string, object?>(item.Key, item.Value?.ToString() ?? string.Empty));
        }
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
