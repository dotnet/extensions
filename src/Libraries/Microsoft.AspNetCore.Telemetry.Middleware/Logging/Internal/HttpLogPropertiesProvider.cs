// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging;

internal static class HttpLogPropertiesProvider
{
    private static readonly ConcurrentDictionary<string, string> _requestPrefixedNamesCache = new();
    private static readonly ConcurrentDictionary<string, string> _responsePrefixedNamesCache = new();

    public static void GetProperties(LogMethodHelper props, IncomingRequestLogRecord logRecord)
    {
        props.Add(HttpLoggingDimensions.Method, logRecord.Method);
        props.Add(HttpLoggingDimensions.Host, logRecord.Host);
        props.Add(HttpLoggingDimensions.Path, logRecord.Path);

        if (logRecord.Duration.HasValue)
        {
            props.Add(HttpLoggingDimensions.Duration, logRecord.Duration.Value);
        }

        if (logRecord.StatusCode.HasValue)
        {
            props.Add(HttpLoggingDimensions.StatusCode, logRecord.StatusCode.Value);
        }

        if (logRecord.PathParameters is not null)
        {
            for (int i = 0; i < logRecord.PathParametersCount; i++)
            {
                var p = logRecord.PathParameters[i];
                props.Add(p.Name, p.Value);
            }
        }

        if (logRecord.RequestBody is not null)
        {
            props.Add(HttpLoggingDimensions.RequestBody, logRecord.RequestBody);
        }

        if (logRecord.ResponseBody is not null)
        {
            props.Add(HttpLoggingDimensions.ResponseBody, logRecord.ResponseBody);
        }

        if (logRecord.RequestHeaders is not null)
        {
            var count = logRecord.RequestHeaders.Count;
            for (int i = 0; i < count; i++)
            {
                var header = logRecord.RequestHeaders[i];
                var prefixedName = _requestPrefixedNamesCache.GetOrAdd(header.Key, static x => HttpLoggingDimensions.RequestHeaderPrefix + x);
                props.Add(prefixedName, header.Value);
            }
        }

        if (logRecord.ResponseHeaders is not null)
        {
            var count = logRecord.ResponseHeaders.Count;
            for (int i = 0; i < count; i++)
            {
                var header = logRecord.ResponseHeaders[i];
                var prefixedName = _responsePrefixedNamesCache.GetOrAdd(header.Key, static x => HttpLoggingDimensions.ResponseHeaderPrefix + x);
                props.Add(prefixedName, header.Value);
            }
        }
    }
}
