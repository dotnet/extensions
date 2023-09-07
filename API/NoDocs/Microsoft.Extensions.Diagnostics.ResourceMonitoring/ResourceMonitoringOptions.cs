// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public class ResourceMonitoringOptions
{
    [TimeSpan(100, 900000)]
    public TimeSpan CollectionWindow { get; set; }
    [TimeSpan(1, 900000)]
    public TimeSpan SamplingInterval { get; set; }
    [Experimental("EXTEXP0008", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [TimeSpan(100, 900000)]
    public TimeSpan CalculationPeriod { get; set; }
    public ResourceMonitoringOptions();
}
