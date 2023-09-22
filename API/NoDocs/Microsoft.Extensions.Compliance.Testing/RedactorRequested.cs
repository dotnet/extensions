// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Testing;

public readonly struct RedactorRequested : IEquatable<RedactorRequested>
{
    public DataClassification DataClassification { get; }
    public int SequenceNumber { get; }
    public RedactorRequested(DataClassification classification, int sequenceNumber);
    public override bool Equals(object? obj);
    public bool Equals(RedactorRequested other);
    public override int GetHashCode();
    public static bool operator ==(RedactorRequested left, RedactorRequested right);
    public static bool operator !=(RedactorRequested left, RedactorRequested right);
}
