// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Metering;

public interface IOutgoingRequestMetricEnricher : IMetricEnricher
{
    IReadOnlyList<string> DimensionNames { get; }
}
