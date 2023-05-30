// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

public readonly struct CheckpointToken
{
    public string Name { get; }
    public int Position { get; }
    public CheckpointToken(string name, int position);
}
