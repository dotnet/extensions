// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Console.Internal;

/// <summary>
/// This type is a variation of the official
/// <see href="https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Logging.Console/src/SimpleConsoleFormatter.cs">SimpleLogFormatter</see>.
/// </summary>
/// <remarks>
/// Contrary to the reference implementation, (1) it does not support padding and instead uses
/// coordinates (e.g. 0:0:1) similarly to how <see cref="IConfiguration" /> manages array keys
/// in configuration dictionary. This is arguably more readable especially when understanding
/// relationship between top-and-nested and/or with enumerations. (2), it is much more colorful
/// to increase readability (e.g. less important information is dimmed, yet still present). And
/// (3), it provides knobs to turn individual elements on/off based on developers preference.
/// </remarks>
internal sealed class LogFormatter : IDisposable
{
    private readonly LogFormatterOptions _options;
    private readonly LogFormatterTheme _theme;

    internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    public LogFormatter(IOptions<LogFormatterOptions> options,
                        IOptions<LogFormatterTheme> theme)
    {
        _options = Throw.IfMemberNull(options, options.Value);
        _theme = Throw.IfMemberNull(theme, theme.Value);
    }

    public void Write(LogEntry<LogEntryCompositeState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        _ = Throw.IfNull(scopeProvider);

        var writer = Throw.IfNull(textWriter);
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

        if (_options.IncludeScopes)
        {
            if (WriteScopes(writer, scopeProvider))
            {
                writer.WriteLine();
            }
        }

        if (_options.IncludeTimestamp)
        {
            WriteTimestamp(writer);
            writer.WriteSpace();
        }

        if (_options.IncludeLogLevel)
        {
            WriteLogLevel(writer, logEntry.LogLevel);
        }

        if (_options.IncludeTraceId)
        {
            writer.Write(logEntry.State.TraceId);
            writer.WriteSpace();
        }

        if (_options.IncludeSpanId)
        {
            writer.Write(logEntry.State.SpanId);
            writer.WriteSpace();
        }

        if (!string.IsNullOrEmpty(message))
        {
            writer.Write(message.Trim());
            writer.WriteSpace();
        }

        if (_options.IncludeCategory)
        {
            WriteCategory(writer, logEntry.Category, logEntry.EventId);
        }

        writer.WriteLine();

        if (logEntry.Exception != null)
        {
            WriteException(writer, logEntry.Exception);
            writer.WriteLine();
        }
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }

    internal bool WriteScopes(TextWriter writer, IExternalScopeProvider scopeProvider)
    {
        var isOneOrMultipleScopes = false;
        var writeScope = WriteScope;

        void WriteScope(object? scope, TextWriter state)
        {
            if (!isOneOrMultipleScopes)
            {
                // Unfortunately there is no way how to know upfront if there
                // is any scope to iterate over, so formatting has to be done
                // from within the for each loop..
                state.WriteLine();
                isOneOrMultipleScopes = true;
            }
            else
            {
                writer.WriteSpace();
            }

            writer.Colorize("{{0}}", _theme.Dimmed, scope);
        }

        scopeProvider.ForEachScope(writeScope, writer);

        return isOneOrMultipleScopes;
    }

    internal void WriteTimestamp(TextWriter writer)
    {
        var now = TimeProvider.GetUtcNow();
        var dateTime = _options.UseUtcTimestamp ? now.DateTime : now.LocalDateTime;

        writer.Write(dateTime.ToString(_options.TimestampFormat, CultureInfo.InvariantCulture));
    }

    internal void WriteLogLevel(TextWriter writer, LogLevel logLevel)
    {
        var color = _theme.ColorsEnabled ? logLevel.InColor() : Colors.None;

        writer.Colorize("{({0})}", color, logLevel.InShortString());
        writer.WriteSpace();
    }

    internal void WriteCategory(TextWriter writer, string category, EventId eventId)
    {
        writer.Colorize("{({0}/{1})}", _theme.Dimmed, category, eventId);
    }

    internal void WriteException(TextWriter writer, Exception? e)
    {
        WriteException(writer, e, new List<int>());
    }

    internal void WriteException(TextWriter writer, Exception? e, IList<int> coordinate)
    {
        if (e == null)
        {
            // This should not happen but lets guard against it.
            return;
        }

        var isTopLevel = coordinate.Count == 0;
        if (isTopLevel)
        {
            coordinate.Add(0);
        }

        writer.WriteCoordinate(coordinate, _theme.Dimmed);
        writer.Colorize("Exception: {{0} ({1})}", _theme.Exception, e.Message, e.GetType().FullName);

        var isStackTrace = !string.IsNullOrWhiteSpace(e.StackTrace);
        if (_options.IncludeExceptionStacktrace && isStackTrace)
        {
            writer.WriteLine();
            writer.Colorize("{{0}}", _theme.ExceptionStackTrace, e.StackTrace);
        }

        if (e is AggregateException ae)
        {
            for (var i = 0; i < ae.InnerExceptions.Count; i++)
            {
                coordinate.Add(i);
                WriteException(writer, ae.InnerExceptions[i], coordinate);
                coordinate.RemoveAt(coordinate.Count - 1);
            }

            return; // Aggregate exception contains synthetic inner exception
        }

        if (e.InnerException != null)
        {
            coordinate.Add(0);
            WriteException(writer, e.InnerException, coordinate);
            coordinate.RemoveAt(coordinate.Count - 1);
        }
    }
}
#endif
