// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public class ResourceMonitoringOptions
{
    [TimeSpan(100, 900000)]
    public TimeSpan CollectionWindow { get; set; }
    [TimeSpan(1, 900000)]
    public TimeSpan SamplingInterval { get; set; }
    [TimeSpan(100, 900000)]
    public TimeSpan PublishingWindow { get; set; }
    [TimeSpan(100, 900000)]
    public TimeSpan CpuConsumptionRefreshInterval { get; set; }
    [TimeSpan(100, 900000)]
    public TimeSpan MemoryConsumptionRefreshInterval { get; set; }
    [Required]
    public ISet<string> SourceIpAddresses { get; set; }
    public ResourceMonitoringOptions();
}
