// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging;

internal static class HttpLogPropertiesProvider
{
    private static readonly ConcurrentDictionary<string, string> _requestPrefixedNamesCache = new();
    private static readonly ConcurrentDictionary<string, string> _responsePrefixedNamesCache = new();

    public static void GetTags(LogMethodHelper helper, IncomingRequestLogRecord logRecord)
    {
        helper.Add(HttpLoggingTagNames.Method, logRecord.Method);
        helper.Add(HttpLoggingTagNames.Host, logRecord.Host);
        helper.Add(HttpLoggingTagNames.Path, logRecord.Path);

        if (logRecord.Duration.HasValue)
        {
            helper.Add(HttpLoggingTagNames.Duration, logRecord.Duration.Value);
        }

        if (logRecord.StatusCode.HasValue)
        {
            helper.Add(HttpLoggingTagNames.StatusCode, logRecord.StatusCode.Value);
        }

        if (logRecord.PathParameters is not null)
        {
            for (int i = 0; i < logRecord.PathParametersCount; i++)
            {
                var p = logRecord.PathParameters[i];
                helper.Add(p.Name, p.Value);
            }
        }

        if (logRecord.RequestBody is not null)
        {
            helper.Add(HttpLoggingTagNames.RequestBody, logRecord.RequestBody);
        }

        if (logRecord.ResponseBody is not null)
        {
            helper.Add(HttpLoggingTagNames.ResponseBody, logRecord.ResponseBody);
        }

        if (logRecord.RequestHeaders is not null)
        {
            var count = logRecord.RequestHeaders.Count;
            for (int i = 0; i < count; i++)
            {
                var header = logRecord.RequestHeaders[i];
                var prefixedName = _requestPrefixedNamesCache.GetOrAdd(header.Key, static x => HttpLoggingTagNames.RequestHeaderPrefix + x);
                helper.Add(prefixedName, header.Value);
            }
        }

        if (logRecord.ResponseHeaders is not null)
        {
            var count = logRecord.ResponseHeaders.Count;
            for (int i = 0; i < count; i++)
            {
                var header = logRecord.ResponseHeaders[i];
                var prefixedName = _responsePrefixedNamesCache.GetOrAdd(header.Key, static x => HttpLoggingTagNames.ResponseHeaderPrefix + x);
                helper.Add(prefixedName, header.Value);
            }
        }
    }
}
