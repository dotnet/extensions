// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

public readonly struct Measure : IEquatable<Measure>
{
    public string Name { get; }
    public long Value { get; }
    public Measure(string name, long value);
    public override bool Equals(object? obj);
    public bool Equals(Measure other);
    public override int GetHashCode();
    public static bool operator ==(Measure left, Measure right);
    public static bool operator !=(Measure left, Measure right);
}
