// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.Telemetry.Metering.Test.Internal;

internal class TestExporter : BaseExporter<Metric>
{
    public Batch<Metric> Metrics { get; set; }

    public override ExportResult Export(in Batch<Metric> batch)
    {
        Metrics = batch;
        return ExportResult.Success;
    }
}
