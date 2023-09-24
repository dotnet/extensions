// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Latency;

public readonly struct TagToken
{
    public string Name { get; }
    public int Position { get; }
    public TagToken(string name, int position);
}
