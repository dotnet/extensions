// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.AspNetCore.Telemetry;

public class RequestLatencyTelemetryOptions
{
    [TimeSpan(1000)]
    public TimeSpan LatencyDataExportTimeout { get; set; }
    public RequestLatencyTelemetryOptions();
}
