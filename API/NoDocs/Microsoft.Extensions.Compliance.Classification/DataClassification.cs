// Assembly 'Microsoft.Extensions.Compliance.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Classification;

public readonly struct DataClassification : IEquatable<DataClassification>
{
    public const ulong NoneTaxonomyValue = 0uL;
    public const ulong UnknownTaxonomyValue = 9223372036854775808uL;
    public static DataClassification None { get; }
    public static DataClassification Unknown { get; }
    public string TaxonomyName { get; }
    public ulong Value { get; }
    public DataClassification(string taxonomyName, ulong value);
    public override bool Equals(object? obj);
    public bool Equals(DataClassification other);
    public override int GetHashCode();
    public static bool operator ==(DataClassification left, DataClassification right);
    public static bool operator !=(DataClassification left, DataClassification right);
    public static DataClassification Combine(DataClassification left, DataClassification right);
    public static DataClassification operator |(DataClassification left, DataClassification right);
}
