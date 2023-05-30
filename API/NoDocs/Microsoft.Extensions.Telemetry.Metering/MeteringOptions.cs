// Assembly 'Microsoft.Extensions.Telemetry'

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Metering;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class MeteringOptions
{
    public int MaxMetricStreams { get; set; }
    public int MaxMetricPointsPerStream { get; set; }
    public MeteringState MeterState { get; set; }
    public IDictionary<string, MeteringState> MeterStateOverrides { get; set; }
    public MeteringOptions();
}
