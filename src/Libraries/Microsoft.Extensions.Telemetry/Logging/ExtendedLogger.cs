// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Logging;

#pragma warning disable CA1031

// NOTE: This implementation uses thread local storage. As a result, it will fail if formatter code, enricher code, or
//       redactor code calls recursively back into the logger. Don't do that.
//
// NOTE: Unlike the original logger in dotnet/runtime, this logger eats exceptions thrown from invoked loggers, enrichers,
//       and redactors, rather than forwarding the exceptions to the caller. The fact an exception occured is recorded in
//       the event log instead. The idea is that failures in the telemetry stack should not lead to failures in the
//       application. It's better to keep running with missing telemetry rather than crashing the process completely.

internal sealed partial class ExtendedLogger : ILogger
{
    private const string ExceptionType = "exception.type";
    private const string ExceptionMessage = "exception.message";
    private const string ExceptionStackTrace = "exception.stacktrace";

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
                exceptions ??= [];
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
            if (loggerInfo.IsNotFilteredOut(logLevel))
            {
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
                    exceptions ??= [];
#pragma warning restore CA1508 // Avoid dead conditional code
                    exceptions.Add(ex);
                }
            }
        }

        HandleExceptions(exceptions);

        return i < loggers.Length;
    }

    private static void HandleExceptions(IEnumerable<Exception>? exceptions)
    {
        if (exceptions != null)
        {
            LoggingEventSource.Instance.LoggingException(new AggregateException("An error occurred while logging.", exceptions));
        }
    }

    private static void RecordException(Exception exception, EnrichmentTagCollector tags, LoggerConfig config)
    {
        tags.Add(ExceptionType, exception.GetType().ToString());

        if (config.IncludeExceptionMessage)
        {
            tags.Add(ExceptionMessage, exception.Message);
        }

        if (config.CaptureStackTraces)
        {
            tags.Add(ExceptionStackTrace, GetExceptionStackTrace(exception, config));
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
            trace = trace.Replace(Environment.NewLine, Environment.NewLine + indentStr + "   ").Trim(' ');
#endif

            _ = sb.Append(exception.GetType());
            _ = sb.Append(": ");

            if (config.IncludeExceptionMessage)
            {
                _ = sb.AppendLine(exception.Message);
                _ = sb.Append(indentStr);
            }

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

        // redact
        JustInTimeRedactor? jitRedactors = null;
        for (int i = 0; i < msgState.ClassifiedTagsCount; i++)
        {
            ref var cp = ref msgState.ClassifiedTagArray[i];
            if (cp.Value != null)
            {
                var jr = JustInTimeRedactor.Get(
                    cp.Value,
                    config.GetRedactor(cp.Classifications),
                    config.AddRedactionDiscriminator ? cp.Name : string.Empty);

                jr.Next = jitRedactors;
                jitRedactors = jr;

                msgState.RedactedTagArray[i] = new(cp.Name, jr);
            }
            else
            {
                msgState.RedactedTagArray[i] = new(cp.Name, null);
            }
        }

        var joiner = ModernJoiner;
        joiner.StaticTags = config.StaticTags;
        joiner.Formatter = formatter;
        joiner.State = msgState;
        joiner.SetIncomingTags(msgState);

        List<Exception>? exceptions = null;

        // enrich
        foreach (var enricher in config.Enrichers)
        {
            try
            {
                enricher(joiner.EnrichmentTagCollector);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        // one last dedicated bit of enrichment
        if (exception != null)
        {
            RecordException(exception, joiner.EnrichmentTagCollector, config);
        }

        for (int i = 0; i < loggers.Length; i++)
        {
            ref readonly MessageLogger loggerInfo = ref loggers[i];
            if (loggerInfo.IsNotFilteredOut(logLevel))
            {
                if (!config.Sampler.ShouldSample(new SamplingParameters(logLevel, loggerInfo.Category!, eventId)))
                {
                    // the record was not selected for being sampled, so we drop it.
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
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }
        }

        joiner.Clear();

        // return the jit redactors to the pool
        while (jitRedactors != null)
        {
            var next = jitRedactors.Next;
            jitRedactors.Return();
            jitRedactors = next;
        }

        HandleExceptions(exceptions);
    }

    private void LegacyPath<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var loggers = MessageLoggers;
        var config = _factory.Config;

        var joiner = LegacyJoiner;
        joiner.StaticTags = config.StaticTags;
        joiner.Formatter = formatter;
        joiner.State = state;

        switch (state)
        {
            case IReadOnlyList<KeyValuePair<string, object?>> stateList:
                joiner.SetIncomingTags(stateList);
                break;

            case IEnumerable<KeyValuePair<string, object?>> stateList:
                joiner.EnrichmentTagCollector.AddRange(stateList);
                break;

            case null:
                break;

            default:
                joiner.EnrichmentTagCollector.Add("{OriginalFormat}", state);
                break;
        }

        List<Exception>? exceptions = null;

        // enrich
        foreach (var enricher in config.Enrichers)
        {
            try
            {
                enricher(joiner.EnrichmentTagCollector);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        // one last dedicated bit of enrichment
        if (exception != null)
        {
            RecordException(exception, joiner.EnrichmentTagCollector, config);
        }

        for (int i = 0; i < loggers.Length; i++)
        {
            ref readonly MessageLogger loggerInfo = ref loggers[i];
            if (loggerInfo.IsNotFilteredOut(logLevel))
            {
                if (!config.Sampler.ShouldSample(new SamplingParameters(logLevel, loggerInfo.Category!, eventId)))
                {
                    // the record was not selected for being sampled, so we drop it.
                    continue;
                }

                try
                {
                    loggerInfo.Logger.Log(logLevel, eventId, joiner, exception, static (s, e) =>
                    {
                        var fmt = (Func<TState, Exception?, string>)s.Formatter!;
                        return fmt((TState)s.State!, e);
                    });
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }
        }

        joiner.Clear();
        HandleExceptions(exceptions);
    }
}
