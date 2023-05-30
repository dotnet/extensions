// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

public readonly struct TagToken
{
    public string Name { get; }
    public int Position { get; }
    public TagToken(string name, int position);
}
