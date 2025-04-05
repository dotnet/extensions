// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

[SupportedOSPlatform("windows")]
internal sealed class PerformanceCounterFactory : IPerformanceCounterFactory
{
    public IPerformanceCounter Create(string categoryName, string counterName, string instanceName)
        => new PerformanceCounterWrapper(categoryName, counterName, instanceName);

    public string[] GetCategoryInstances(string categoryName)
    {
        var category = new PerformanceCounterCategory(categoryName);
        string[] instanceNames = category.GetInstanceNames();
        return instanceNames == null || instanceNames.Length == 0 ? [] : instanceNames;
    }
}
