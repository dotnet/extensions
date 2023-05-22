// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using OpenTelemetry;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;

internal sealed class TestTraceProcessor : BaseProcessor<Activity>
{
    public bool IsProcessorInvoked { get; set; }

    public Activity? FirstActivity { get; set; }

    public override void OnEnd(Activity activity)
    {
        FirstActivity = activity;
        IsProcessorInvoked = true;
    }
}
