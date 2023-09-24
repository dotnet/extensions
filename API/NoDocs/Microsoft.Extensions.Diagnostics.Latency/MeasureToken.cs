// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Latency;

public readonly struct MeasureToken
{
    public string Name { get; }
    public int Position { get; }
    public MeasureToken(string name, int position);
}
