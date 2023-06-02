// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#if !NET5_0_OR_GREATER
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;
#endif

using System.IO;
using OpenTelemetry;
using OpenTelemetry.Logs;

#if NET5_0_OR_GREATER
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Telemetry.Console.Internal;
using MSOptions = Microsoft.Extensions.Options;
#endif

using static System.Console;

namespace Microsoft.Extensions.Telemetry.Console;

[SuppressMessage("Minor Vulnerability", "S2228:Console logging should not be used", Justification = "We need to use here console logging.")]
[SuppressMessage("Major Code Smell", "S106:Standard outputs should not be used directly to log anything", Justification = "We need to use here console logging.")]
internal sealed class LoggingConsoleExporter : BaseExporter<LogRecord>
{
    internal const string OriginalFormat = "{OriginalFormat}";

#if NET5_0_OR_GREATER
    private readonly LogFormatter _consoleFormatter;
    private readonly LoggingConsoleOptions _formatterOptions;

    public LoggingConsoleExporter(MSOptions.IOptions<LoggingConsoleOptions> loggingConsoleOptions)
    {
        _formatterOptions = loggingConsoleOptions.Value;

        _consoleFormatter = new LogFormatter(
            MSOptions.Options.Create(
                new LogFormatterOptions
                {
                    IncludeScopes = _formatterOptions.IncludeScopes,
                    IncludeCategory = _formatterOptions.IncludeCategory,
                    IncludeExceptionStacktrace = _formatterOptions.IncludeExceptionStacktrace,
                    IncludeLogLevel = _formatterOptions.IncludeLogLevel,
                    IncludeTimestamp = _formatterOptions.IncludeTimestamp,
                    IncludeSpanId = _formatterOptions.IncludeSpanId,
                    IncludeTraceId = _formatterOptions.IncludeTraceId,
                    TimestampFormat = _formatterOptions.TimestampFormat!,
                    UseUtcTimestamp = _formatterOptions.UseUtcTimestamp,
                    IncludeDimensions = _formatterOptions.IncludeDimensions,
                }),
            MSOptions.Options.Create(
                new LogFormatterTheme
                {
                    ColorsEnabled = _formatterOptions.ColorsEnabled,
                    Dimmed = new ColorSet(_formatterOptions.DimmedColor, _formatterOptions.DimmedBackgroundColor),
                    Exception = new ColorSet(_formatterOptions.ExceptionColor, _formatterOptions.ExceptionBackgroundColor),
                    ExceptionStackTrace = new ColorSet(_formatterOptions.ExceptionStackTraceColor, _formatterOptions.ExceptionStackTraceBackgroundColor),
                    Dimensions = new ColorSet(_formatterOptions.DimensionsColor, _formatterOptions.DimensionsBackgroundColor),
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
            if (_formatterOptions.IncludeScopes)
            {
                WriteScopesLine(logRecord, textWriter);
            }

            formattedMessage = logRecord.FormattedMessage;
            var logEntry = new LogEntry<LogEntryCompositeState>(
                logRecord.LogLevel,
                logRecord.CategoryName!,
                logRecord.EventId,
                new LogEntryCompositeState(logRecord.StateValues, logRecord.TraceId, logRecord.SpanId),
                logRecord.Exception,
                FormatLog);

            _consoleFormatter.Write(logEntry, textWriter);

            var text = textWriter.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                System.Console.Write(text);
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

    private static void WriteScopesLine(
        LogRecord logRecord,
#if NET5_0_OR_GREATER
        StringWriter writer)
#else
        TextWriter writer)
#endif
    {
        var hasScopes = false;

        logRecord.ForEachScope((scope, _) =>
        {
            foreach (var subScope in scope)
            {
                if (!hasScopes)
                {
                    hasScopes = true;
                    writer.Write("Scope:");
                }

                var scopeText = string.IsNullOrEmpty(subScope.Key)
                    ? $"{subScope.Value}"
                    : $"{subScope.Key}:{subScope.Value}";

                writer.Write($" {scopeText}");
            }
        }, default(object));

        if (hasScopes)
        {
            writer.WriteLine();
        }
    }

#if !NET5_0_OR_GREATER
    private static void WriteDimensions(LogRecord logRecord)
    {
        if (logRecord.StateValues != null)
        {
            foreach (var kvp in logRecord.StateValues)
            {
                if (kvp.Key != LoggingConsoleExporter.OriginalFormat)
                {
                    System.Console.WriteLine($"  {kvp.Key}={kvp.Value}");
                }
            }
        }
    }

    private void WriteBatchOfLogRecords(Batch<LogRecord> batch)
    {
        foreach (var logRecord in batch)
        {
            WriteScopesLine(logRecord, System.Console.Out);

            var message = logRecord.FormattedMessage;
            if (string.IsNullOrEmpty(message))
            {
                message = GetOriginalFormat(logRecord.StateValues);
            }

            System.Console.WriteLine(
                $"{logRecord.Timestamp:yyyy-MM-dd HH:mm:ss.fff} {logRecord.LogLevel} {logRecord.TraceId} {logRecord.SpanId} {message} {logRecord.CategoryName}/{logRecord.EventId}");

            if (logRecord.Exception is not null)
            {
                var exceptionMessage = GetExceptionMessage(logRecord.Exception);
                System.Console.Write(exceptionMessage);
            }

            WriteDimensions(logRecord);
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
}
