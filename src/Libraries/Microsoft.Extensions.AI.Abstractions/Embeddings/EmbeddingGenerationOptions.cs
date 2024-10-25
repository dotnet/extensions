// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for an embedding generation request.</summary>
public class EmbeddingGenerationOptions
{
    private int? _dimensions;

    /// <summary>Gets or sets the number of dimensions requested in the embedding.</summary>
    public int? Dimensions
    {
        get => _dimensions;
        set
        {
            if (value is not null)
            {
                _ = Throw.IfLessThan(value.Value, 1);
            }

            _dimensions = value;
        }
    }

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
            Dimensions = Dimensions,
            AdditionalProperties = AdditionalProperties?.Clone(),
        };
}
