// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class BufferedLoggerProxy : IBufferedLogger
{
    private readonly ExtendedLogger _parentLogger;

    public BufferedLoggerProxy(ExtendedLogger parentLogger)
    {
        _parentLogger = parentLogger;
    }

    public void LogRecords(IEnumerable<BufferedLogRecord> records)
    {
        LoggerInformation[] loggerInformations = _parentLogger.Loggers;
        foreach (LoggerInformation loggerInformation in loggerInformations)
        {
            ILogger iLogger = loggerInformation.Logger;
            if (iLogger is IBufferedLogger bufferedLogger)
            {
                bufferedLogger.LogRecords(records);
            }
            else
            {
                foreach (BufferedLogRecord record in records)
                {
#pragma warning disable CA2201 // Do not raise reserved exception types
                    iLogger.Log(
                        record.LogLevel,
                        record.EventId,
                        record.Attributes,
                        record.Exception is not null ? new Exception(record.Exception) : null,
                        (_, _) => record.FormattedMessage ?? string.Empty);
#pragma warning restore CA2201 // Do not raise reserved exception types
                }
            }
        }
    }
}
