// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.Common'

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

[Experimental("EXTEXP0007", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class TelemetryHealthCheckPublisherOptions
{
    public bool LogOnlyUnhealthy { get; set; }
    public TelemetryHealthCheckPublisherOptions();
}
