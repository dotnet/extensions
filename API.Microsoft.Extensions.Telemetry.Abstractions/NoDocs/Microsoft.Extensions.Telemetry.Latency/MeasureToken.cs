// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

public readonly struct MeasureToken
{
    public string Name { get; }
    public int Position { get; }
    public MeasureToken(string name, int position);
}
