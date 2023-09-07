// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

public readonly struct Checkpoint : IEquatable<Checkpoint>
{
    public string Name { get; }
    public long Elapsed { get; }
    public long Frequency { get; }
    public Checkpoint(string name, long elapsed, long frequency);
    public override bool Equals(object? obj);
    public bool Equals(Checkpoint other);
    public override int GetHashCode();
    public static bool operator ==(Checkpoint left, Checkpoint right);
    public static bool operator !=(Checkpoint left, Checkpoint right);
}
