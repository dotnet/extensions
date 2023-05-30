// Assembly 'Microsoft.Extensions.Compliance.Testing'

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Testing;

public static class SimpleClassifications
{
    public static string TaxonomyName { get; }
    public static DataClassification PrivateData { get; }
    public static DataClassification PublicData { get; }
}
