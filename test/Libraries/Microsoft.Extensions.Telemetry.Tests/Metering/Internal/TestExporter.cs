// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.Telemetry.Metering.Test.Internal;

internal class TestExporter : BaseExporter<Metric>
{
    private readonly string[]? _meterNamePrefixes;

    public TestExporter()
        : this(null!)
    {
    }

    public TestExporter(params string[] meterNamePrefixes)
    {
        _meterNamePrefixes = meterNamePrefixes;
    }

    public Batch<Metric> Metrics { get; set; }

    public override ExportResult Export(in Batch<Metric> batch)
    {
        if (_meterNamePrefixes == null)
        {
            Metrics = batch;
        }
        else
        {
            var filterdMetrics = new List<Metric>();

            foreach (var metric in batch)
            {
                if (ContainsMatchingPrefix(metric.MeterName))
                {
                    filterdMetrics.Add(metric);
                }
            }

            Metrics = new Batch<Metric>(filterdMetrics.ToArray(), filterdMetrics.Count);
        }

        return ExportResult.Success;
    }

    private bool ContainsMatchingPrefix(string meterName)
    {
        if (_meterNamePrefixes == null)
        {
            return true;
        }

        foreach (var meterNamePrefix in _meterNamePrefixes)
        {
            if (meterName.StartsWith(meterNamePrefix))
            {
                return true;
            }
        }

        return false;
    }
}
