// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

/// <summary>
/// Interface for performance counters.
/// </summary>
internal interface IPerformanceCounter
{
    /// <summary>
    /// Gets the name of the performance counter category.
    /// </summary>
    string InstanceName { get; }

    /// <summary>
    /// Get the next value of the performance counter.
    /// </summary>
    /// <returns>The next value of the performance counter.</returns>
    float NextValue();
}
