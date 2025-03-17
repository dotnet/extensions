using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal static class SerializedLogRecordFactory
{
    private static readonly ObjectPool<List<KeyValuePair<string, object?>>> _attributesPool =
        PoolFactory.CreateListPool<KeyValuePair<string, object?>>();

    public static SerializedLogRecord Create(
        LogLevel logLevel,
        EventId eventId,
        DateTimeOffset timestamp,
        IReadOnlyList<KeyValuePair<string, object?>> attributes,
        Exception? exception,
        string formattedMessage)
    {
        int sizeInBytes = 0;
        List<KeyValuePair<string, object?>> serializedAttributes = _attributesPool.Get();
        for (int i = 0; i < attributes.Count; i++)
        {
            string key = attributes[i].Key;
            string value = attributes[i].Value?.ToString() ?? string.Empty;

            sizeInBytes += key.Length * sizeof(char);
            sizeInBytes += value.Length * sizeof(char);

            serializedAttributes.Add(new KeyValuePair<string, object?>(key, value));
        }

        string exceptionMessage = string.Empty;
        if (exception is not null)
        {
            sizeInBytes += exception.Message.Length * sizeof(char);
            exceptionMessage = exception.Message;
        }

        sizeInBytes += formattedMessage.Length * sizeof(char);

        return new SerializedLogRecord(
            logLevel,
            eventId,
            timestamp,
            serializedAttributes,
            exceptionMessage,
            formattedMessage,
            sizeInBytes);
    }

    public static void Return(SerializedLogRecord[] bufferedRecords)
    {
        for (int i = 0; i < bufferedRecords.Length; i++)
        {
            _attributesPool.Return(bufferedRecords[i].Attributes);
        }
    }

    public static void Return(SerializedLogRecord bufferedRecord)
    {
        _attributesPool.Return(bufferedRecord.Attributes);
    }
}
