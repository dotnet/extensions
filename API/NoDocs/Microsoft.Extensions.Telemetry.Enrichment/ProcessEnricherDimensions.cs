// Assembly 'Microsoft.Extensions.Telemetry'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Enrichment;

public static class ProcessEnricherDimensions
{
    public const string ProcessId = "pid";
    public const string ThreadId = "tid";
    public static IReadOnlyList<string> DimensionNames { get; }
}
