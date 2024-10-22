// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

internal static partial class Log
{
    internal const int IdMaximumPayloadBytesExceeded = 1;
    internal const int IdSerializationFailure = 2;
    internal const int IdKeyEmptyOrWhitespace = 3;
    internal const int IdMaximumKeyLengthExceeded = 4;

    [LoggerMessage(LogLevel.Warning, "Cache MaximumPayloadBytes ({bytes}) exceeded", EventId = IdMaximumPayloadBytesExceeded)]
    internal static partial void MaximumPayloadBytesExceeded(this ILogger logger, Exception e, int bytes);

    [LoggerMessage(LogLevel.Warning, "Cache serialization failure", EventId = IdSerializationFailure)]
    internal static partial void SerializationFailure(this ILogger logger, Exception e);

    [LoggerMessage(LogLevel.Warning, "Cache key empty or whitespace", EventId = IdKeyEmptyOrWhitespace)]
    internal static partial void KeyEmptyOrWhitespace(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Cache key maximum length exceeded (maximum: {maxLength}, actual: {keyLength})", EventId = IdMaximumKeyLengthExceeded)]
    internal static partial void MaximumKeyLengthExceeded(this ILogger logger, int maxLength, int keyLength);
}

internal static partial class Log
{
    // placeholder because I'm struggling to get the code-generator for [LoggerMessage] working, unknown for now
    internal static partial void MaximumPayloadBytesExceeded(this ILogger logger, Exception e, int bytes)
    {
        if (logger is not null && logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(LogLevel.Warning, IdMaximumPayloadBytesExceeded, bytes,
                e, static (state, e) => $"Cache MaximumPayloadBytes ({state}) exceeded");
        }
    }

    internal static partial void SerializationFailure(this ILogger logger, Exception e)
    {
        if (logger is not null && logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(LogLevel.Warning, IdSerializationFailure, 0,
                e, static (state, e) => $"Cache serialization failure");
        }
    }

    internal static partial void KeyEmptyOrWhitespace(this ILogger logger)
    {
        if (logger is not null && logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(LogLevel.Warning, IdKeyEmptyOrWhitespace, 0,
                null, static (state, e) => $"Cache key empty or whitespace");
        }
    }

    internal static partial void MaximumKeyLengthExceeded(this ILogger logger, int maxLength, int keyLength)
    {
        if (logger is not null && logger.IsEnabled(LogLevel.Warning))
        {
            logger.Log(LogLevel.Warning, IdMaximumKeyLengthExceeded, (maxLength, keyLength),
                null, static (state, e) => $"Cache key maximum length exceeded (maximum: {state.maxLength}, actual: {state.keyLength})");
        }
    }
}
