// Assembly 'Microsoft.Extensions.Compliance.Testing'

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Simple data classifications.
/// </summary>
public static class SimpleClassifications
{
    /// <summary>
    /// Gets the name of this classification taxonomy.
    /// </summary>
    public static string TaxonomyName { get; }

    /// <summary>
    /// Gets the private data classification.
    /// </summary>
    public static DataClassification PrivateData { get; }

    /// <summary>
    /// Gets the public data classification.
    /// </summary>
    public static DataClassification PublicData { get; }
}
