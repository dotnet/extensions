// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Shared.Pools;
using OpenTelemetry.Logs;

namespace Microsoft.Extensions.Telemetry.Logging;

internal sealed class Logger : ILogger
{
    internal static readonly Func<IExternalScopeProvider?, DateTime, string, LogLevel, EventId, string?, object?,
        Exception?, IReadOnlyList<KeyValuePair<string, object?>>?, LogRecord> CreateLogRecord = GetLogCreator();

    private const string ExceptionStackTrace = "stackTrace";
    private readonly string _categoryName;
    private readonly LoggerProvider _provider;

    /// <summary>
    /// Call OpenTelemetry's LogRecord constructor.
    /// </summary>
    /// <remarks>
    /// Reflection is used because the constructor has 'internal' modifier and cannot be called directly.
    /// This will be replaced with a direct call in one of the two conditions below.
    ///  - LogRecord will make its internalsVisible to R9 library.
    ///  - LogRecord constructor will become public.
    /// </remarks>
    private static Func<IExternalScopeProvider?, DateTime, string, LogLevel, EventId, string?, object?, Exception?, IReadOnlyList<KeyValuePair<string, object?>>?, LogRecord> GetLogCreator()
    {
        var logRecordConstructor = typeof(LogRecord).GetConstructor(
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            BindingFlags.Instance | BindingFlags.NonPublic,
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            null,

            new[]
            {
                typeof(IExternalScopeProvider),
                typeof(DateTime),
                typeof(string),
                typeof(LogLevel),
                typeof(EventId),
                typeof(string),
                typeof(object),
                typeof(Exception),
                typeof(IReadOnlyList<KeyValuePair<string, object?>>)
            },
            null)!;

        var val = new[]
        {
            Expression.Parameter(typeof(IExternalScopeProvider)),
            Expression.Parameter(typeof(DateTime)),
            Expression.Parameter(typeof(string)),
            Expression.Parameter(typeof(LogLevel)),
            Expression.Parameter(typeof(EventId)),
            Expression.Parameter(typeof(string)),
            Expression.Parameter(typeof(object)),
            Expression.Parameter(typeof(Exception)),
            Expression.Parameter(typeof(IReadOnlyList<KeyValuePair<string, object?>>))
        };

        var lambdaLogRecord = Expression.Lambda<Func<IExternalScopeProvider?, DateTime, string, LogLevel,
            EventId, string?, object?, Exception?, IReadOnlyList<KeyValuePair<string, object?>>?,
            LogRecord>>(Expression.New(logRecordConstructor, val), val);

        return lambdaLogRecord.Compile();
    }

    internal static TimeProvider TimeProvider => TimeProvider.System;

    internal Logger(string categoryName, LoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    internal IExternalScopeProvider? ScopeProvider { get; set; }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        LogMethodHelper propertyBag;
        LogMethodHelper? rentedHelper = null;

        try
        {
            if (state is LogMethodHelper helper && _provider.CanUsePropertyBagPool)
            {
                propertyBag = helper;
            }
            else
            {
                rentedHelper = GetHelper();
                propertyBag = rentedHelper;

                switch (state)
                {
                    case IReadOnlyList<KeyValuePair<string, object?>> stateList:
                        rentedHelper.AddRange(stateList);
                        break;

                    case IEnumerable<KeyValuePair<string, object?>> stateList:
                        rentedHelper.AddRange(stateList);
                        break;

                    case null:
                        break;

                    default:
                        rentedHelper.Add("{OriginalFormat}", state);
                        break;
                }
            }

            foreach (var enricher in _provider.Enrichers)
            {
                enricher.Enrich(propertyBag);
            }

            if (exception != null && _provider.IncludeStackTrace)
            {
                propertyBag.Add(ExceptionStackTrace, GetExceptionStackTrace(exception, _provider.MaxStackTraceLength));
            }

            var record = CreateLogRecord(
                _provider.IncludeScopes ? ScopeProvider : null,
                TimeProvider.GetUtcNow().UtcDateTime,
                _categoryName,
                logLevel,
                eventId,
                _provider.UseFormattedMessage ? formatter(state, exception) : null,

                // This parameter needs to be null for OpenTelemetry.Exporter.Geneva to pick up LogRecord.StateValues (the last parameter).
                // This is equivalent to using OpenTelemetryLogger with ParseStateValues option set to true.
                null,
                exception,
                propertyBag);

            _provider.Processor?.OnEnd(record);
        }
        catch (Exception ex)
        {
            LoggingEventSource.Log.LogException(ex);
            throw;
        }
        finally
        {
            ReturnHelper(rentedHelper);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

#pragma warning disable CS8633
#pragma warning disable CS8766
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
#pragma warning restore CS8633
#pragma warning restore CS8766
    {
        ScopeProvider ??= new LoggerExternalScopeProvider();

        return ScopeProvider.Push(state);
    }

    private static string GetExceptionStackTrace(Exception exception, int maxStackTraceLength)
    {
        if (exception.StackTrace == null && exception.InnerException == null)
        {
            return string.Empty;
        }

        var stackTrace = string.Empty;
        var stringBuilder = PoolFactory.SharedStringBuilderPool.Get();
        _ = stringBuilder.AppendLine(exception.StackTrace);

        try
        {
            if (exception.InnerException != null)
            {
                GetInnerExceptionTrace(exception, stringBuilder, maxStackTraceLength);
            }

            if (stringBuilder.Length > maxStackTraceLength)
            {
                stackTrace = stringBuilder.ToString(0, maxStackTraceLength);
            }
            else
            {
                stackTrace = stringBuilder.ToString();
            }
        }
        finally
        {
            PoolFactory.SharedStringBuilderPool.Return(stringBuilder);
        }

        return stackTrace;
    }

    private static void GetInnerExceptionTrace(Exception exception, StringBuilder stringBuilder, int maxStackTraceLength)
    {
        var innerException = exception.InnerException;
        if (innerException != null && stringBuilder.Length < maxStackTraceLength)
        {
            _ = stringBuilder.Append("InnerException type:");
            _ = stringBuilder.Append(innerException.GetType());
            _ = stringBuilder.Append(" message:");
            _ = stringBuilder.Append(innerException.Message);
            _ = stringBuilder.Append(" stack:");
            _ = stringBuilder.Append(innerException.StackTrace);

            GetInnerExceptionTrace(innerException, stringBuilder, maxStackTraceLength);
        }
    }

    private LogMethodHelper GetHelper()
    {
        return _provider.CanUsePropertyBagPool
            ? LogMethodHelper.GetHelper()
            : new LogMethodHelper();
    }

    private void ReturnHelper(LogMethodHelper? helper)
    {
        if (_provider.CanUsePropertyBagPool && helper != null)
        {
            LogMethodHelper.ReturnHelper(helper);
        }
    }
}
