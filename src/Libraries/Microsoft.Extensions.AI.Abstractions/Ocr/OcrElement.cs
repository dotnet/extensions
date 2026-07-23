// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a single positioned element within an <see cref="OcrPage"/>, such as a block, table, or image.</summary>
/// <remarks>
/// Elements appear in <see cref="OcrPage.Elements"/> in reading order, so a consumer can walk a page as one
/// heterogeneous stream and project the kinds it cares about with
/// <see cref="System.Linq.Enumerable.OfType{TResult}(System.Collections.IEnumerable)"/> (for example
/// <c>page.Elements.OfType&lt;OcrTable&gt;()</c>). The full page text is available directly on
/// <see cref="OcrPage.Text"/>. The base is shaped to be promotable to a future shared document-element type.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(OcrBlock), typeDiscriminator: "block")]
[JsonDerivedType(typeof(OcrTable), typeDiscriminator: "table")]
[JsonDerivedType(typeof(OcrImage), typeDiscriminator: "image")]
public abstract class OcrElement
{
    /// <summary>Initializes a new instance of the <see cref="OcrElement"/> class.</summary>
    protected OcrElement()
    {
    }

    /// <summary>Gets or sets the region of the page the element occupies, when the engine provides geometry.</summary>
    public OcrBoundingRegion? BoundingRegion { get; set; }

    /// <summary>Gets or sets the confidence for the element in the range [0, 1], when available.</summary>
    public double? Confidence { get; set; }

    /// <summary>Gets or sets the provider-native object underlying this element.</summary>
    /// <remarks>
    /// If an <see cref="OcrElement"/> is created to represent an underlying object from another object model,
    /// this property can store that original object. This can be useful for debugging or for enabling a
    /// consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the element.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
