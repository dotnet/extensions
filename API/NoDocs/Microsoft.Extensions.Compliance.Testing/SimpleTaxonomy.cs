// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;

namespace Microsoft.Extensions.Compliance.Testing;

[Flags]
public enum SimpleTaxonomy : ulong
{
    None = 0uL,
    PublicData = 1uL,
    PrivateData = 2uL,
    Unknown = 9223372036854775808uL
}
