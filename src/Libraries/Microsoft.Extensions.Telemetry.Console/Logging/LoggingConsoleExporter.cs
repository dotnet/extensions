// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#if !NET5_0_OR_GREATER
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;
#endif

using OpenTelemetry;
using OpenTelemetry.Logs;

#if NET5_0_OR_GREATER
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Telemetry.Console.Internal;
using Microsoft.Shared.Diagnostics;
using MSOptions = Microsoft.Extensions.Options;
#endif

using static System.Console;

namespace Microsoft.Extensions.Telemetry.Console;

[SuppressMessage("Minor Vulnerability", "S2228:Console logging should not be used", Justification = "We need to use here console logging.")]
[SuppressMessage("Major Code Smell", "S106:Standard outputs should not be used directly to log anything", Justification = "We need to use here console logging.")]
internal sealed class LoggingConsoleExporter : BaseExporter<LogRecord>
{
    private const string OriginalFormat = "{OriginalFormat}";

#if NET5_0_OR_GREATER
    private readonly LogFormatter _consoleFormatter;

    public LoggingConsoleExporter(MSOptions.IOptions<LoggingConsoleOptions> consoleLogFormatterOptions)
    {
        var formatterOptions = Throw.IfNullOrMemberNull(consoleLogFormatterOptions, consoleLogFormatterOptions?.Value);

        _consoleFormatter = new LogFormatter(
            MSOptions.Options.Create(
                new LogFormatterOptions
                {
                    IncludeScopes = formatterOptions.IncludeScopes,
                    IncludeCategory = formatterOptions.IncludeCategory,
                    IncludeExceptionStacktrace = formatterOptions.IncludeExceptionStacktrace,
                    IncludeLogLevel = formatterOptions.IncludeLogLevel,
                    IncludeTimestamp = formatterOptions.IncludeTimestamp,
                    IncludeSpanId = formatterOptions.IncludeSpanId,
                    IncludeTraceId = formatterOptions.IncludeTraceId,
                    TimestampFormat = formatterOptions.TimestampFormat!,
                    UseUtcTimestamp = formatterOptions.UseUtcTimestamp
                }),
            MSOptions.Options.Create(
                new LogFormatterTheme
                {
                    ColorsEnabled = formatterOptions.ColorsEnabled,
                    Dimmed = new ColorSet(formatterOptions.DimmedColor, formatterOptions.DimmedBackgroundColor),
                    Exception = new ColorSet(formatterOptions.ExceptionColor, formatterOptions.ExceptionBackgroundColor),
                    ExceptionStackTrace = new ColorSet(formatterOptions.ExceptionStackTraceColor, formatterOptions.ExceptionStackTraceBackgroundColor)
                }));
    }
#endif

    public override ExportResult Export(in Batch<LogRecord> batch)
    {
#if NET5_0_OR_GREATER
        string? formattedMessage;

        string FormatLog(LogEntryCompositeState compositeState, Exception? exception) =>
            string.IsNullOrEmpty(formattedMessage)
                ? GetOriginalFormat(compositeState.State)
                : formattedMessage;

        using var textWriter = new StringWriter();
        foreach (var logRecord in batch)
        {
            formattedMessage = logRecord.FormattedMessage;
            var logEntry = new LogEntry<LogEntryCompositeState>(
                logRecord.LogLevel,
                logRecord.CategoryName!,
                logRecord.EventId,
                new LogEntryCompositeState(logRecord.StateValues, logRecord.TraceId, logRecord.SpanId),
                logRecord.Exception,
                FormatLog);

            var scopedProvider = new LoggerExternalScopeProvider();
            _consoleFormatter.Write(logEntry, scopedProvider, textWriter);

            WriteScopesLine(logRecord);

            var text = textWriter.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                Write(text);
            }
        }

        return ExportResult.Success;
#else
        WriteBatchOfLogRecords(batch);
        return ExportResult.Success;
#endif
    }

#if NET5_0_OR_GREATER
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _consoleFormatter.Dispose();
        }

        base.Dispose(disposing);
    }
#endif

    private static string GetOriginalFormat(IReadOnlyCollection<KeyValuePair<string, object?>>? state)
    {
        if (state is null)
        {
            return string.Empty;
        }

        foreach (var item in state)
        {
            if (item.Key == OriginalFormat)
            {
                return item.Value?.ToString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

#if !NET5_0_OR_GREATER
    private void WriteBatchOfLogRecords(Batch<LogRecord> batch)
    {
        foreach (var logRecord in batch)
        {
            WriteScopesLine(logRecord);

            var message = logRecord.FormattedMessage;
            if (string.IsNullOrEmpty(message))
            {
                message = GetOriginalFormat(logRecord.StateValues);
            }

            WriteLine($"{logRecord.Timestamp:yyyy-MM-dd HH:mm:ss.fff} {logRecord.LogLevel} {logRecord.TraceId} {logRecord.SpanId} {message} {logRecord.CategoryName}/{logRecord.EventId}");

            if (logRecord.Exception is not null)
            {
                var exceptionMessage = GetExceptionMessage(logRecord.Exception);
                Write(exceptionMessage);
            }
        }
    }

    private string GetExceptionMessage(Exception ex, string tabulation = "")
    {
        var result = PoolFactory.SharedStringBuilderPool.Get();
        try
        {
            var str = $"{tabulation}Exception: {ex.Message} {ex.GetType().FullName}.{Environment.NewLine}";
            _ = result.Append(str);

            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                _ = result.AppendLine(ex.StackTrace);
            }

            if (ex is AggregateException ae)
            {
                for (var i = 0; i < ae.InnerExceptions.Count; i++)
                {
                    _ = result.Append(GetExceptionMessage(ae.InnerExceptions[i], $"{tabulation}\t"));
                }

                return result.ToString();
            }

            if (ex.InnerException is not null)
            {
                _ = result.Append(GetExceptionMessage(ex.InnerException, $"{tabulation}\t"));
            }

            return result.ToString();
        }
        finally
        {
            PoolFactory.SharedStringBuilderPool.Return(result);
        }
    }
#endif

    private void WriteScopesLine(LogRecord logRecord)
    {
        var isMultipleScopes = false;

        void WriteScope(KeyValuePair<string, object?> scope)
        {
            var text = string.IsNullOrEmpty(scope.Key)
                ? $" {scope.Value}"
                : $" {scope.Key}:{scope.Value}";

            if (!isMultipleScopes)
            {
                isMultipleScopes = true;
                Write($"Scope:{text}");
            }
            else
            {
                Write(text);
            }
        }

        logRecord.ForEachScope((scope, _) =>
        {
            foreach (var subScope in scope)
            {
                WriteScope(subScope);
            }
        }, this);

        if (logRecord.StateValues is not null)
        {
            foreach (var stateValue in logRecord.StateValues)
            {
                if (stateValue.Key != OriginalFormat)
                {
                    WriteScope(stateValue);
                }
            }
        }

        if (isMultipleScopes)
        {
            WriteLine();
        }
    }
}
