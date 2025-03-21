// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Provides a base class for all content used with AI services.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DataContent), typeDiscriminator: "data")]
[JsonDerivedType(typeof(ErrorContent), typeDiscriminator: "error")]
[JsonDerivedType(typeof(FunctionCallContent), typeDiscriminator: "functionCall")]
[JsonDerivedType(typeof(FunctionResultContent), typeDiscriminator: "functionResult")]
[JsonDerivedType(typeof(TextContent), typeDiscriminator: "text")]
[JsonDerivedType(typeof(UriContent), typeDiscriminator: "uri")]
[JsonDerivedType(typeof(UsageContent), typeDiscriminator: "usage")]
public class AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContent"/> class.
    /// </summary>
    protected AIContent()
    {
    }

    /// <summary>Gets or sets the raw representation of the content from an underlying implementation.</summary>
    /// <remarks>
    /// If an <see cref="AIContent"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets additional properties for the content.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
