// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for an embedding generation request.</summary>
public class EmbeddingGenerationOptions
{
    /// <summary>Gets or sets the model ID for the embedding generation request.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets additional properties for the embedding generation request.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Produces a clone of the current <see cref="EmbeddingGenerationOptions"/> instance.</summary>
    /// <returns>A clone of the current <see cref="EmbeddingGenerationOptions"/> instance.</returns>
    /// <remarks>
    /// The clone will have the same values for all properties as the original instance. Any collections, like <see cref="AdditionalProperties"/>
    /// are shallow-cloned, meaning a new collection instance is created, but any references contained by the collections are shared with the original.
    /// </remarks>
    public virtual EmbeddingGenerationOptions Clone() =>
        new()
        {
            ModelId = ModelId,
            AdditionalProperties = AdditionalProperties?.Clone(),
        };
}
