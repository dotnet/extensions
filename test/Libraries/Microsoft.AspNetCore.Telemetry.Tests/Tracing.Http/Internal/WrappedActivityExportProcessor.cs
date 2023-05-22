// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using OpenTelemetry;

namespace Microsoft.AspNetCore.Telemetry.Test.Internal;

internal sealed class WrappedActivityExportProcessor : SimpleActivityExportProcessor
{
    public bool IsInvoked { get; set; }

    public Activity? FirstActivity { get; private set; }

    public WrappedActivityExportProcessor(BaseExporter<Activity> exporter)
        : base(exporter)
    {
    }

    public override void OnEnd(Activity data)
    {
        base.OnEnd(data);

        FirstActivity ??= data;
        IsInvoked = true;
    }
}
