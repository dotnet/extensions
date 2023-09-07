// Assembly 'Microsoft.Extensions.Compliance.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Compliance.Classification;

/// <summary>
/// Base attribute for data classification.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = true)]
public class DataClassificationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the notes.
    /// </summary>
    /// <remarks>Optional free-form text to provide context during a privacy audit.</remarks>
    public string Notes { get; set; }

    /// <summary>
    /// Gets the data class represented by this attribute.
    /// </summary>
    public DataClassification Classification { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute" /> class.
    /// </summary>
    /// <param name="classification">The data classification to apply.</param>
    protected DataClassificationAttribute(DataClassification classification);
}
