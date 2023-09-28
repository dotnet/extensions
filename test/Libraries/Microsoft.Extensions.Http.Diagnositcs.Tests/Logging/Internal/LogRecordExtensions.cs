// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Http.Logging.Internal;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Http.Logging.Test.Internal;

internal static class LogRecordExtensions
{
    public static Dictionary<string, string?> GetStructuredState(this FakeLogRecord logRecord)
    {
        Assert.NotNull(logRecord.StructuredState);
        Assert.NotEmpty(logRecord.StructuredState);
        return logRecord.StructuredState.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static string GetEnrichmentProperty(this LogRecord logRecord, string name)
    {
        return logRecord.EnrichmentTags!.FirstOrDefault(kvp => kvp.Key == name).Value!.ToString()!;
    }

    public static void Contains(this IDictionary<string, string?> logRecordState, string key, string expectedValue)
    {
        var actualValue = Assert.Contains(key, logRecordState);
        Assert.Equal(expectedValue, actualValue);
    }

    public static void Contains(this IDictionary<string, string?> logRecordState, string key, Action<string?> assertion)
    {
        var actualValue = Assert.Contains(key, logRecordState);
        assertion(actualValue);
    }

    public static void NotContains(this IDictionary<string, string?> logRecord, string key)
    {
        Assert.DoesNotContain(key, logRecord);
    }
}
