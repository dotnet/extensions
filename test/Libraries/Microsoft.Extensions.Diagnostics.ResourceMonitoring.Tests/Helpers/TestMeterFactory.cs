// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;

internal class TestMeterFactory : IMeterFactory
{
    public List<Meter> Meters { get; } = new List<Meter>();

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options.Name, options.Version, Array.Empty<KeyValuePair<string, object?>>(), scope: this);
        Meters.Add(meter);

        return meter;
    }

    public Meter Create(string name)
    {
        return Create(new MeterOptions(name)
        {
            Version = null,
            Tags = null,
            Scope = null
        });
    }

    public void Dispose()
    {
        foreach (var meter in Meters)
        {
            meter.Dispose();
        }

        Meters.Clear();
    }
}
