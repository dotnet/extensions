// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Microsoft.Extensions.Telemetry.Logging.Test.Internals;

internal class TestProcessor : BaseProcessor<LogRecord>
{
    internal int DisposeCalledTimes;

    protected override void Dispose(bool disposing)
    {
        DisposeCalledTimes += 1;
        base.Dispose(disposing);
    }
}
