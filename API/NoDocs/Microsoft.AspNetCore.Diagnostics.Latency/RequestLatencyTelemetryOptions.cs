// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.AspNetCore.Diagnostics.Latency;

public class RequestLatencyTelemetryOptions
{
    [TimeSpan(1000)]
    public TimeSpan LatencyDataExportTimeout { get; set; }
    public RequestLatencyTelemetryOptions();
}
