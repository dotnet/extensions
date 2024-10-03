// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal static partial class Log
{
    internal const int IdMaximumPayloadBytesExceeded = 1;
    internal const int IdSerializationFailure = 2;
    internal const int IdDeserializationFailure = 3;
    internal const int IdKeyEmptyOrWhitespace = 4;
    internal const int IdMaximumKeyLengthExceeded = 5;
    internal const int IdCacheBackendReadFailure = 6;
    internal const int IdCacheBackendWriteFailure = 7;

    [LoggerMessage(LogLevel.Error, "Cache MaximumPayloadBytes ({bytes}) exceeded", EventId = IdMaximumPayloadBytesExceeded)]
    internal static partial void MaximumPayloadBytesExceeded(this ILogger logger, Exception e, int bytes);

    // note that serialization is critical enough that we perform hard failures in addition to logging; serialization
    // failures are unlikely to be transient (i.e. connectivity); we would rather this shows up in QA, rather than
    // being invisible and people *thinking* they're using cache, when actually they are not

    [LoggerMessage(LogLevel.Error, "Cache serialization failure", EventId = IdSerializationFailure)]
    internal static partial void SerializationFailure(this ILogger logger, Exception e);

    // (see same notes per SerializationFailure)
    [LoggerMessage(LogLevel.Error, "Cache deserialization failure", EventId = IdDeserializationFailure)]
    internal static partial void DeserializationFailure(this ILogger logger, Exception e);

    [LoggerMessage(LogLevel.Error, "Cache key empty or whitespace", EventId = IdKeyEmptyOrWhitespace)]
    internal static partial void KeyEmptyOrWhitespace(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Cache key maximum length exceeded (maximum: {maxLength}, actual: {keyLength})", EventId = IdMaximumKeyLengthExceeded)]
    internal static partial void MaximumKeyLengthExceeded(this ILogger logger, int maxLength, int keyLength);

    [LoggerMessage(LogLevel.Error, "Cache backend read failure", EventId = IdCacheBackendReadFailure)]
    internal static partial void CacheBackendReadFailure(this ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Cache backend write failure", EventId = IdCacheBackendWriteFailure)]
    internal static partial void CacheBackendWriteFailure(this ILogger logger, Exception ex);
}

internal static partial class Log
{
    // placeholder because I'm struggling to get the code-generator for [LoggerMessage] working, unknown for now
    internal static partial void MaximumPayloadBytesExceeded(this ILogger logger, Exception e, int bytes)
    {
        if (logger is not null && logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(LogLevel.Error, IdMaximumPayloadBytesExceeded, bytes,
                e, static (state, e) => $"Cache MaximumPayloadBytes ({state}) exceeded");
        }
    }

    internal static partial void SerializationFailure(this ILogger logger, Exception e)
    {
        if (logger is not null && logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(LogLevel.Error, IdSerializationFailure, 0,
                e, static (state, e) => $"Cache serialization failure");
        }
    }

    internal static partial void DeserializationFailure(this ILogger logger, Exception e)
    {
        if (logger is not null && logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(LogLevel.Error, IdDeserializationFailure, 0,
                e, static (state, e) => $"Cache deserialization failure");
        }
    }

    internal static partial void KeyEmptyOrWhitespace(this ILogger logger)
    {
        if (logger is not null && logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(LogLevel.Error, IdKeyEmptyOrWhitespace, 0,
                null, static (state, e) => $"Cache key empty or whitespace");
        }
    }

    internal static partial void MaximumKeyLengthExceeded(this ILogger logger, int maxLength, int keyLength)
    {
        if (logger is not null && logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(LogLevel.Error, IdMaximumKeyLengthExceeded, (maxLength, keyLength),
                null, static (state, e) => $"Cache key maximum length exceeded (maximum: {state.maxLength}, actual: {state.keyLength})");
        }
    }

    internal static partial void CacheBackendReadFailure(this ILogger logger, Exception e)
    {
        if (logger is not null && logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(LogLevel.Error, IdCacheBackendReadFailure, 0,
                e, static (state, e) => $"Cache backend read failure");
        }
    }

    internal static partial void CacheBackendWriteFailure(this ILogger logger, Exception e)
    {
        if (logger is not null && logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(LogLevel.Error, IdCacheBackendWriteFailure, 0,
                e, static (state, e) => $"Cache backend write failure");
        }
    }
}
