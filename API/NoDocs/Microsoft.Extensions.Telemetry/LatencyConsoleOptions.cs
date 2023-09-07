// Assembly 'Microsoft.Extensions.Telemetry'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry;

public class LatencyConsoleOptions
{
    public bool OutputCheckpoints { get; set; }
    public bool OutputTags { get; set; }
    public bool OutputMeasures { get; set; }
    public LatencyConsoleOptions();
}
