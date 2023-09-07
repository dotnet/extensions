// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

public readonly struct Tag
{
    public string Name { get; }
    public string Value { get; }
    public Tag(string name, string value);
}
