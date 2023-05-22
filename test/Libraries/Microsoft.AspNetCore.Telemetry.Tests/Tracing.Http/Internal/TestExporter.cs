// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using OpenTelemetry;

namespace Microsoft.AspNetCore.Telemetry.Test.Internal;

internal sealed class TestExporter : BaseExporter<Activity>
{
    public bool IsInvoked { get; set; }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        IsInvoked = true;
        return ExportResult.Success;
    }
}
