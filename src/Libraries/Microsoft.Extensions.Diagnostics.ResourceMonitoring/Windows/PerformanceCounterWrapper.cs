// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

[SupportedOSPlatform("windows")]
internal sealed class PerformanceCounterWrapper : IPerformanceCounter
{
    private readonly PerformanceCounter _counter;

    internal PerformanceCounterWrapper(string categoryName, string counterName, string instanceName)
    {
        _counter = new PerformanceCounter(categoryName, counterName, instanceName, readOnly: true);
        InstanceName = instanceName;
    }

    public string InstanceName { get; }

    public float NextValue() => _counter.NextValue();
}
