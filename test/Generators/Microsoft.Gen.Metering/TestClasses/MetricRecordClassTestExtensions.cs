// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metering;

namespace TestClasses
{
    internal partial record class MetricRecordClassTestExtensions(string Name, string Address)
    {
        [Counter<int>]
        public static partial CounterFromRecordClass CreateCounterFromRecordClass(Meter meter);
    }
}
