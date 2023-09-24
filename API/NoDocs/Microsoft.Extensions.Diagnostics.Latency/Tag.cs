// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Latency;

public readonly struct Tag
{
    public string Name { get; }
    public string Value { get; }
    public Tag(string name, string value);
}
