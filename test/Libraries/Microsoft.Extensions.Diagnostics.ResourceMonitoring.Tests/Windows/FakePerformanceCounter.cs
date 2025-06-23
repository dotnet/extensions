// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

public class FakePerformanceCounter(string instanceName, float[] values) : IPerformanceCounter
{
#pragma warning disable S3604 // Member initializer values should not be redundant
    private readonly object _lock = new();
#pragma warning restore S3604
    private int _index;

    public string InstanceName => instanceName;

    public float NextValue()
    {
        lock (_lock)
        {
            if (_index >= values.Length)
            {
                throw new InvalidOperationException("No more values available.");
            }

            return values[_index++];
        }
    }
}
