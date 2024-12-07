// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.JsonExceptionConverter;
using Microsoft.Shared.Pools;
using static Microsoft.Extensions.Logging.ExtendedLoggerFactory;

namespace Microsoft.Extensions.Logging;
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

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
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
                            exception = JsonSerializer.Deserialize<Exception>(serializedRecord.Exception, ExceptionConverter.JsonSerializerOptions);
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
#endif
