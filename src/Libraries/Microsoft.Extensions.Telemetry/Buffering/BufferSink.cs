// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.ExceptionJsonConverter;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class BufferSink : IBufferSink
{
    private readonly ExtendedLoggerFactory _factory;
    private readonly string _category;
    private readonly ObjectPool<List<PooledLogRecord>> _logRecordPool = PoolFactory.CreateListPool<PooledLogRecord>();

    public BufferSink(ExtendedLoggerFactory factory, string category)
    {
        _factory = factory;
        _category = category;
    }

    public void LogRecords<T>(IEnumerable<T> serializedRecords)
        where T : ISerializedLogRecord
    {
        var providers = _factory.ProviderRegistrations;

        List<PooledLogRecord>? pooledList = null;
        try
        {
            foreach (var provider in providers)
            {
                var logger = provider.Provider.CreateLogger(_category);

                if (logger is IBufferedLogger bufferedLogger)
                {
                    if (pooledList is null)
                    {
                        pooledList = _logRecordPool.Get();

                        foreach (var serializedRecord in serializedRecords)
                        {
                            pooledList.Add(
                                new PooledLogRecord(
                                    serializedRecord.Timestamp,
                                    serializedRecord.LogLevel,
                                    serializedRecord.EventId,
                                    serializedRecord.Exception,
                                    serializedRecord.FormattedMessage,
                                    serializedRecord.Attributes.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray()));
                        }
                    }

                    bufferedLogger.LogRecords(pooledList);

                }
                else
                {
                    foreach (var serializedRecord in serializedRecords)
                    {
                        Exception? exception = null;
                        if (serializedRecord.Exception is not null)
                        {
                            exception = JsonSerializer.Deserialize(serializedRecord.Exception, ExceptionJsonContext.Default.Exception);
                        }

                        logger.Log(
                            serializedRecord.LogLevel,
                            serializedRecord.EventId,
                            serializedRecord.Attributes,
                            exception,
                            (_, _) => serializedRecord.FormattedMessage ?? string.Empty);
                    }
                }
            }
        }
        finally
        {
            if (pooledList is not null)
            {
                _logRecordPool.Return(pooledList);
            }
        }
    }
}
