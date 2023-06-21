// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Telemetry.Logging;

// NOTE: This implementation currently depends on thread-local storage. As a result,
//       it is not resilient to logger reentrancy. Reentrancy can happen if a formatting
//       function called by ILogger.Log ends up calling back into the logging system.

#pragma warning disable CA1031

internal sealed partial class ExtendedLogger : ILogger
{
    private const string ExceptionStackTrace = "stackTrace";

    private readonly ILogger _nextLogger;
    private readonly ExtendedLoggerFactory _factory;

    public ExtendedLogger(ILogger nextLogger, ExtendedLoggerFactory factory)
    {
        _nextLogger = nextLogger;
        _factory = factory;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return _nextLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _nextLogger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (typeof(TState) == typeof(LoggerMessageState))
        {
            ModernPath(logLevel, eventId, (LoggerMessageState?)(object?)state, exception, (Func<LoggerMessageState, Exception?, string>)(object)formatter);
        }
        else
        {
            LegacyPath<TState>(logLevel, eventId, state, exception, formatter);
        }
    }

    private static string GetExceptionStackTrace(Exception exception, LoggerConfig config)
    {
        const int IndentAmount = 3;

        var sb = PoolFactory.SharedStringBuilderPool.Get();
        try
        {
            HandleException(exception, 0);

            return sb.Length > config.MaxStackTraceLength
                ? sb.ToString(0, config.MaxStackTraceLength)
                : sb.ToString();
        }
        finally
        {
            PoolFactory.SharedStringBuilderPool.Return(sb);
        }

        void HandleException(Exception exception, int indent)
        {
            var indentStr = new string(' ', indent);

            if (sb.Length > 0)
            {
                _ = sb.Append(indentStr);
                _ = sb.Append("---> ");
            }

            var trace = new StackTrace(exception, config.UseFileInfoForStackTraces).ToString();

#if NETCOREAPP3_1_OR_GREATER
            trace = trace.Replace(Environment.NewLine, Environment.NewLine + indentStr + "   ", StringComparison.Ordinal).Trim(' ');
#else
#pragma warning disable R9A044 // Assign array of literal values to a static field for improved performance
            trace = trace.Replace(Environment.NewLine, Environment.NewLine + indentStr + "   ").Trim(' ');
#pragma warning restore R9A044 // Assign array of literal values to a static field for improved performance
#endif

            _ = sb.Append(exception.GetType().ToString());
            _ = sb.Append(": ");
            _ = sb.AppendLine(exception.Message);
            _ = sb.Append(indentStr);
            _ = sb.Append(trace);

            if (exception is AggregateException aggregateException)
            {
                foreach (var ex in aggregateException.InnerExceptions)
                {
                    HandleException(ex, indent + IndentAmount);
                }
            }
            else if (exception.InnerException != null)
            {
                HandleException(exception.InnerException, indent + IndentAmount);
            }
        }
    }

    private void ModernPath(LogLevel logLevel, EventId eventId, LoggerMessageState? msgState, Exception? exception, Func<LoggerMessageState, Exception?, string> formatter)
    {
        // presume warning/error/critical levels are enabled, to avoid the call to IsEnabled
        if (logLevel < LogLevel.Warning)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
        }

        msgState ??= MessageProps;

        var config = _factory.Config;

        var joiner = Joiner;
        joiner.DynamicProperties = msgState;
        joiner.Formatter = formatter;
        joiner.StaticProperties = config.StaticProperties;

        try
        {
            // redact
            foreach (var cp in msgState.ClassifiedProperties)
            {
                msgState.AddProperty(cp.Name, config.RedactorProvider(cp.Classification).Redact(cp.Value));
            }

            // enrich
            foreach (var enricher in config.Enrichers)
            {
                enricher(msgState.EnrichmentPropertyBag);
            }

            // one last dedicated bit of enrichment
            if (exception != null && config.CaptureStackTraces)
            {
                msgState.AddProperty(ExceptionStackTrace, GetExceptionStackTrace(exception, config));
            }

            _nextLogger.Log(logLevel, eventId, joiner, exception, static (s, e) => s.Formatter(s.DynamicProperties, e));
        }
        catch (Exception ex)
        {
            LoggingEventSource.Log.LogException(ex);
        }
    }

    private void LegacyPath<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // presume warning/error/critical levels are enabled, to avoid the call to IsEnabled
        if (logLevel < LogLevel.Warning)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
        }

        var config = _factory.Config;

        var bag = Bag;
        bag.Formatter = formatter;
        bag.State = state;
        bag.StaticProperties = config.StaticProperties;

        switch (state)
        {
            case IReadOnlyList<KeyValuePair<string, object?>> stateList:
                bag.DynamicProperties = stateList;
                break;

            case IEnumerable<KeyValuePair<string, object?>> stateList:
                bag.AddRange(stateList);
                break;

            case null:
                break;

            default:
                bag.Add("{OriginalFormat}", state);
                break;
        }

        try
        {
            // enrich
            foreach (var enricher in config.Enrichers)
            {
                enricher(bag);
            }

            // one last dedicated bit of enrichment
            if (exception != null && config.CaptureStackTraces)
            {
                bag.Add(ExceptionStackTrace, GetExceptionStackTrace(exception, config));
            }

            _nextLogger.Log(logLevel, eventId, bag, exception, static (s, e) =>
            {
                var fmt = (Func<TState, Exception?, string>)s.Formatter!;
                return fmt((TState)s.State!, e);
            });
        }
        catch (Exception ex)
        {
            LoggingEventSource.Log.LogException(ex);
        }
    }
}
