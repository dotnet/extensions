// Assembly 'Microsoft.Extensions.Compliance.Abstractions'

namespace Microsoft.Extensions.Compliance.Classification;

/// <summary>
/// Indicates data which is specifically not classified.
/// </summary>
public sealed class NoDataClassificationAttribute : DataClassificationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Compliance.Classification.NoDataClassificationAttribute" /> class.
    /// </summary>
    public NoDataClassificationAttribute();
}
