// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Classes of data used for simple scenarios.
/// </summary>
[Flags]
public enum SimpleTaxonomy : ulong
{
    /// <summary>
    /// No data classification.
    /// </summary>
    None = 0uL,
    /// <summary>
    /// This is public data.
    /// </summary>
    PublicData = 1uL,
    /// <summary>
    /// This is private data.
    /// </summary>
    PrivateData = 2uL,
    /// <summary>
    /// Unknown data classification, handle with care.
    /// </summary>
    Unknown = 9223372036854775808uL
}
