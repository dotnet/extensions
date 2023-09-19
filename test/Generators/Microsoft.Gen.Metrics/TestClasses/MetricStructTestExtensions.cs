// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace TestClasses
{
    internal partial struct MetricStructTestExtensions
    {
        [Counter<int>]
        public static partial CounterFromStruct CreateCounterFromStruct(Meter meter);
    }
}
