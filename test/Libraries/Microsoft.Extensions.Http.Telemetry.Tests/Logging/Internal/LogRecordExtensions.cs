// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Http.Telemetry.Logging.Internal;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test.Internal;

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

    public static void Contains(this Dictionary<string, string?> logRecord, string key, string value)
    {
        Assert.True(logRecord.ContainsKey(key));
        Assert.Equal(value, logRecord[key]);
    }

    public static void NotContains(this Dictionary<string, string?> logRecord, string key)
    {
        Assert.False(logRecord.ContainsKey(key));
    }
}
