// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Telemetry.Metering;

public class EventCountersCollectorOptions
{
    [Required]
    public IDictionary<string, ISet<string>> Counters { get; set; }
    [TimeSpan("00:00:01", "00:10:00")]
    public TimeSpan SamplingInterval { get; set; }
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public bool IncludeRecommendedDefault { get; set; }
    public EventCountersCollectorOptions();
}
