// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Testing;

public readonly struct RedactedData : IEquatable<RedactedData>
{
    public string Original { get; }
    public string Redacted { get; }
    public int SequenceNumber { get; }
    public RedactedData(string original, string redacted, int sequenceNumber);
    public override bool Equals(object? obj);
    public bool Equals(RedactedData other);
    public override int GetHashCode();
    public static bool operator ==(RedactedData left, RedactedData right);
    public static bool operator !=(RedactedData left, RedactedData right);
}
