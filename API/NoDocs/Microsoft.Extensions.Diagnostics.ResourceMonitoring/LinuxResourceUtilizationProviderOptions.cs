// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public class LinuxResourceUtilizationProviderOptions
{
    [TimeSpan(100, 900000)]
    public TimeSpan CpuConsumptionRefreshInterval { get; set; }
    [TimeSpan(100, 900000)]
    public TimeSpan MemoryConsumptionRefreshInterval { get; set; }
    public LinuxResourceUtilizationProviderOptions();
}
