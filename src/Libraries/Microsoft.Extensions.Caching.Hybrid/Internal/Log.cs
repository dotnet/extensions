// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using static Microsoft.Extensions.Caching.Hybrid.Internal.HybridCachePayload;

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
    internal const int IdKeyInvalidContent = 8;
    internal const int IdKeyInvalidUnicode = 9;
    internal const int IdTagInvalidUnicode = 10;
    internal const int IdCacheBackendDataRejected = 11;

    [LoggerMessage(LogLevel.Error, "Cache MaximumPayloadBytes ({Bytes}) exceeded.", EventName = "MaximumPayloadBytesExceeded", EventId = IdMaximumPayloadBytesExceeded, SkipEnabledCheck = false)]
    internal static partial void MaximumPayloadBytesExceeded(this ILogger logger, Exception e, int bytes);

    // note that serialization is critical enough that we perform hard failures in addition to logging; serialization
    // failures are unlikely to be transient (i.e. connectivity); we would rather this shows up in QA, rather than
    // being invisible and people *thinking* they're using cache, when actually they are not

    [LoggerMessage(LogLevel.Error, "Cache serialization failure.", EventName = "SerializationFailure", EventId = IdSerializationFailure, SkipEnabledCheck = false)]
    internal static partial void SerializationFailure(this ILogger logger, Exception e);

    // (see same notes per SerializationFailure)
    [LoggerMessage(LogLevel.Error, "Cache deserialization failure.", EventName = "DeserializationFailure", EventId = IdDeserializationFailure, SkipEnabledCheck = false)]
    internal static partial void DeserializationFailure(this ILogger logger, Exception e);

    [LoggerMessage(LogLevel.Error, "Cache key empty or whitespace.", EventName = "KeyEmptyOrWhitespace", EventId = IdKeyEmptyOrWhitespace, SkipEnabledCheck = false)]
    internal static partial void KeyEmptyOrWhitespace(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Cache key maximum length exceeded (maximum: {MaxLength}, actual: {KeyLength}).", EventName = "MaximumKeyLengthExceeded",
        EventId = IdMaximumKeyLengthExceeded, SkipEnabledCheck = false)]
    internal static partial void MaximumKeyLengthExceeded(this ILogger logger, int maxLength, int keyLength);

    [LoggerMessage(LogLevel.Error, "Cache backend read failure.", EventName = "CacheBackendReadFailure", EventId = IdCacheBackendReadFailure, SkipEnabledCheck = false)]
    internal static partial void CacheUnderlyingDataQueryFailure(this ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Cache backend write failure.", EventName = "CacheBackendWriteFailure", EventId = IdCacheBackendWriteFailure, SkipEnabledCheck = false)]
    internal static partial void CacheBackendWriteFailure(this ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Cache key contains invalid content.", EventName = "KeyInvalidContent", EventId = IdKeyInvalidContent, SkipEnabledCheck = false)]
    internal static partial void KeyInvalidContent(this ILogger logger); // for PII etc reasons, we won't include the actual key

    [LoggerMessage(LogLevel.Error, "Key contains malformed unicode.",
        EventName = "KeyInvalidUnicode", EventId = IdKeyInvalidUnicode, SkipEnabledCheck = false)]
    internal static partial void KeyInvalidUnicode(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Tag contains malformed unicode.",
        EventName = "TagInvalidUnicode", EventId = IdTagInvalidUnicode, SkipEnabledCheck = false)]
    internal static partial void TagInvalidUnicode(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Cache backend data rejected: {reason}.",
        EventName = "CacheBackendDataRejected", EventId = IdCacheBackendDataRejected, SkipEnabledCheck = false)]
    internal static partial void CacheBackendDataRejected(this ILogger logger, HybridCachePayloadParseResult reason, Exception? ex);
}
