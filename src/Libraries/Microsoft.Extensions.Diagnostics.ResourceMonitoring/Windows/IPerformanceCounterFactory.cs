// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

/// <summary>
/// Factory interface for creating performance counters.
/// </summary>
internal interface IPerformanceCounterFactory
{
    /// <summary>
    /// Creates a performance counter.
    /// </summary>
    /// <param name="categoryName">The name of the performance counter category.</param>
    /// <param name="counterName">The name of the performance counter.</param>
    /// <param name="instanceName">The name of the instance of the performance counter.</param>
    /// <returns>A new instance of <see cref="IPerformanceCounter"/>.</returns>
    IPerformanceCounter Create(string categoryName, string counterName, string instanceName);

    /// <summary>
    /// Gets the names of all instances of a performance counter category.
    /// </summary>
    /// <param name="categoryName">PerformanceCounter category name.</param>
    /// <returns>Array of instance names.</returns>
    string[] GetCategoryInstances(string categoryName);
}
