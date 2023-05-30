// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.AspNetCore.Telemetry;

public interface IIncomingRequestMetricEnricher : IMetricEnricher
{
    IReadOnlyList<string> DimensionNames { get; }
}
