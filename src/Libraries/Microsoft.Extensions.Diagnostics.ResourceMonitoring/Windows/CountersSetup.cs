// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// A static class for performance counters preparation.
/// </summary>
public static class CountersSetup
{
    /// <summary>
    /// Setup Category function.
    /// </summary>
    [ExcludeFromCodeCoverage]
#pragma warning disable IDE0079 // Remove unnecessary suppression
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Indeed, this whole class is for Windows only")]
    [SuppressMessage("Major Code Smell", "S2589:Boolean expressions should not be gratuitous", Justification = "We allow to setup initial boolean value even if it may not be changed under if-block")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
    public static void PreparePerformanceCounters()
    {
        var exists = PerformanceCounterCategory.Exists(WindowsPerfCounterConstants.ContainerCounterCategoryName);
        var counterTypeChanged = false;
        if (exists)
        {
            var counterSet = PerformanceCounterCategory
            .GetCategories()
            .Where(cat => cat.CategoryName == WindowsPerfCounterConstants.ContainerCounterCategoryName)
            .Select(cat => cat.GetInstanceNames().Length > 0
                    ? cat.GetInstanceNames().Select(i => cat.GetCounters(i)).SelectMany(counter => counter)
                    : cat.GetCounters(string.Empty)).SelectMany(counter => counter)
            .Where(counter => counter.CounterName == WindowsPerfCounterConstants.CpuUtilizationCounterName
                    && counter.CounterType == PerformanceCounterType.RawFraction);

            counterTypeChanged = !(counterSet == null || !counterSet.Any());

            if (counterTypeChanged)
            {
                PerformanceCounterCategory.Delete(WindowsPerfCounterConstants.ContainerCounterCategoryName);
            }
        }

        if (!exists || counterTypeChanged)
        {
            var counterDataCollection = new CounterCreationDataCollection();

            // Add the counter.
            var cpuUtilization = new CounterCreationData
            {
                CounterType = PerformanceCounterType.Timer100Ns,
                CounterName = WindowsPerfCounterConstants.CpuUtilizationCounterName
            };
            _ = counterDataCollection.Add(cpuUtilization);

            var memUtilization = new CounterCreationData
            {
                CounterType = PerformanceCounterType.RawFraction,
                CounterName = WindowsPerfCounterConstants.MemoryUtilizationCounterName
            };
            _ = counterDataCollection.Add(memUtilization);

            var memLimit = new CounterCreationData
            {
                CounterType = PerformanceCounterType.RawBase,
                CounterName = WindowsPerfCounterConstants.MemoryLimitCounterName
            };
            _ = counterDataCollection.Add(memLimit);

            // Create the category.
            _ = PerformanceCounterCategory.Create(WindowsPerfCounterConstants.ContainerCounterCategoryName,
                WindowsPerfCounterConstants.ContainerCounterCategoryName,
                PerformanceCounterCategoryType.MultiInstance, counterDataCollection);
        }
    }
}
