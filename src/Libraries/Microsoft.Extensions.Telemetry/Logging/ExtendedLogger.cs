﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Telemetry.Logging;

#pragma warning disable CA1031

internal sealed partial class ExtendedLogger : ILogger
{
    private const string ExceptionStackTrace = "stackTrace";

    private readonly ExtendedLoggerFactory _factory;

    public LoggerInformation[] Loggers { get; set; }
    public MessageLogger[] MessageLoggers { get; set; } = Array.Empty<MessageLogger>();
    public ScopeLogger[] ScopeLoggers { get; set; } = Array.Empty<ScopeLogger>();

    public ExtendedLogger(ExtendedLoggerFactory factory, LoggerInformation[] loggers)
    {
        _factory = factory;
        Loggers = loggers;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (typeof(TState) == typeof(LoggerMessageState))
        {
            var msgState = (LoggerMessageState?)(object?)state;
            if (msgState != null)
            {
                ModernPath(logLevel, eventId, msgState, exception, (Func<LoggerMessageState, Exception?, string>)(object)formatter);
                return;
            }
        }

        LegacyPath<TState>(logLevel, eventId, state, exception, formatter);
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        var loggers = ScopeLoggers;

        if (loggers.Length == 0)
        {
            return NullScope.Instance;
        }
        else if (loggers.Length == 1)
        {
            return loggers[0].CreateScope(state);
        }

        var scope = new Scope(loggers.Length);
        List<Exception>? exceptions = null;
        for (int i = 0; i < loggers.Length; i++)
        {
            try
            {
                scope.SetDisposable(i, loggers[i].CreateScope(state));
            }
            catch (Exception ex)
            {
#pragma warning disable CA1508 // Avoid dead conditional code
                exceptions ??= new();
#pragma warning restore CA1508 // Avoid dead conditional code
                exceptions.Add(ex);
            }
        }

        HandleExceptions(exceptions);

        return scope;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        var loggers = MessageLoggers;

        List<Exception>? exceptions = null;
        int i = 0;
        for (; i < loggers.Length; i++)
        {
            ref readonly MessageLogger loggerInfo = ref loggers[i];
            if (!loggerInfo.IsEnabled(logLevel))
            {
                continue;
            }

            try
            {
                if (loggerInfo.LoggerIsEnabled(logLevel))
                {
                    break;
                }
            }
            catch (Exception ex)
            {
#pragma warning disable CA1508 // Avoid dead conditional code
                exceptions ??= new();
#pragma warning restore CA1508 // Avoid dead conditional code
                exceptions.Add(ex);
            }
        }

        HandleExceptions(exceptions);

        return i < loggers.Length;
    }

    private static void HandleExceptions(IEnumerable<Exception>? exceptions)
    {
        if (exceptions != null)
        {
            LoggingEventSource.Log.LogException(new AggregateException("An error occurred while logging.", exceptions));
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

    private void ModernPath(LogLevel logLevel, EventId eventId, LoggerMessageState msgState, Exception? exception, Func<LoggerMessageState, Exception?, string> formatter)
    {
        var loggers = MessageLoggers;
        var config = _factory.Config;

        var joiner = Joiner;
        joiner.StaticProperties = config.StaticProperties;
        joiner.Formatter = formatter;
        joiner.SetIncomingProperties(msgState);

        List<Exception>? exceptions = null;
        try
        {
            // redact
            foreach (var cp in msgState.ClassifiedProperties)
            {
                joiner.Add(cp.Name, config.RedactorProvider(cp.Classification).Redact(cp.Value));
            }

            // enrich
            foreach (var enricher in config.Enrichers)
            {
                enricher(joiner);
            }

            // one last dedicated bit of enrichment
            if (exception != null && config.CaptureStackTraces)
            {
                joiner.Add(ExceptionStackTrace, GetExceptionStackTrace(exception, config));
            }

            for (int i = 0; i < loggers.Length; i++)
            {
                ref readonly MessageLogger loggerInfo = ref loggers[i];
                if (!loggerInfo.IsEnabled(logLevel))
                {
                    continue;
                }

                try
                {
                    loggerInfo.LoggerLog(logLevel, eventId, joiner, exception, static (s, e) =>
                    {
                        var fmt = s.Formatter!;
                        return fmt(s.State!, e);
                    });
                }
                catch (Exception ex)
                {
                    exceptions ??= new();
                    exceptions.Add(ex);
                }
            }
        }
        catch (Exception ex)
        {
            exceptions ??= new();
            exceptions.Add(ex);
        }

        HandleExceptions(exceptions);
        joiner.Clear();
    }

    private void LegacyPath<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var loggers = MessageLoggers;
        var config = _factory.Config;

        var bag = Bag;
        bag.StaticProperties = config.StaticProperties;
        bag.Formatter = formatter;
        bag.State = state;

        switch (state)
        {
            case IReadOnlyList<KeyValuePair<string, object?>> stateList:
                bag.SetIncomingProperties(stateList);
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

        List<Exception>? exceptions = null;
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

            for (int i = 0; i < loggers.Length; i++)
            {
                ref readonly MessageLogger loggerInfo = ref loggers[i];
                if (!loggerInfo.IsEnabled(logLevel))
                {
                    continue;
                }

                try
                {
                    loggerInfo.Logger.Log(logLevel, eventId, bag, exception, static (s, e) =>
                    {
                        var fmt = (Func<TState, Exception?, string>)s.Formatter!;
                        return fmt((TState)s.State!, e);
                    });
                }
                catch (Exception ex)
                {
                    exceptions ??= new();
                    exceptions.Add(ex);
                }
            }
        }
        catch (Exception ex)
        {
            exceptions ??= new();
            exceptions.Add(ex);
        }

        HandleExceptions(exceptions);
        bag.Clear();
    }
}
