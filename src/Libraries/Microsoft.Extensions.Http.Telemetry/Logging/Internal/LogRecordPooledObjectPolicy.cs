// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

internal sealed class LogRecordPooledObjectPolicy : PooledObjectPolicy<LogRecord>
{
    public override LogRecord Create() => new();

    public override bool Return(LogRecord record)
    {
        record.Host = string.Empty;
        record.Method = null;
        record.Path = string.Empty;
        record.Duration = 0;
        record.StatusCode = null;
        record.RequestBody = string.Empty;
        record.ResponseBody = string.Empty;
        record.EnrichmentTags = null;
        record.RequestHeaders = null;
        record.ResponseHeaders = null;
        return true;
    }
}
