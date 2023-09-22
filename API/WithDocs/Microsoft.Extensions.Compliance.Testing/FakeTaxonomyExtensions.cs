// Assembly 'Microsoft.Extensions.Compliance.Testing'

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Extensions for working with the simple data classification taxonomy.
/// </summary>
public static class FakeTaxonomyExtensions
{
    /// <summary>
    /// Gets the taxonomy value associated with a particular data classification.
    /// </summary>
    /// <param name="classification">The data classification of interest.</param>
    /// <returns>The resulting taxonomy value for the given data classification.</returns>
    public static FakeTaxonomy AsFakeTaxonomy(this DataClassification classification);
}
