// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an annotation on a span of content.
/// </summary>
[JsonDerivedType(typeof(CitationAnnotation), typeDiscriminator: "citation")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
public class AIAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAnnotation"/> class.
    /// </summary>
    public AIAnnotation()
    {
    }

    /// <summary>
    /// Gets or sets the start character index (inclusive) of the annotated span in the <see cref="AIContent"/>.
    /// </summary>
    public int? StartIndex { get; set; }

    /// <summary>
    /// Gets or sets the end character index (exclusive) of the annotated span in the <see cref="AIContent"/>.
    /// </summary>
    public int? EndIndex { get; set; }

    /// <summary>Gets or sets the raw representation of the annotation from an underlying implementation.</summary>
    /// <remarks>
    /// If an <see cref="AIAnnotation"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model, if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>
    /// Gets or sets additional metadata specific to the provider or source type.
    /// </summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
