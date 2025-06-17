// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;
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
                _ = Throw.IfLessThan(value.Value, 1, nameof(value));
            }

            _dimensions = value;
        }
    }

    /// <summary>Gets or sets the model ID for the embedding generation request.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets additional properties for the embedding generation request.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the embedding generation options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="IEmbeddingGenerator" /> implementation may have its own representation of options.
    /// When <see cref="IEmbeddingGenerator{TInput, TEmbedding}.GenerateAsync" /> 
    /// is invoked with an <see cref="EmbeddingGenerationOptions" />, that implementation may convert the provided options into
    /// its own representation in order to use it while performing the operation. For situations where a consumer knows
    /// which concrete <see cref="IEmbeddingGenerator" /> is being used and how it represents options, a new instance of that
    /// implementation-specific options type may be returned by this callback, for the <see cref="IEmbeddingGenerator" />
    /// implementation to use instead of creating a new instance. Such implementations may mutate the supplied options
    /// instance further based on other settings supplied on this <see cref="EmbeddingGenerationOptions" /> instance or from other inputs,
    /// therefore, it is <b>strongly recommended</b> to not return shared instances and instead make the callback return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly-typed
    /// properties on <see cref="EmbeddingGenerationOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<IEmbeddingGenerator, object?>? RawRepresentationFactory { get; set; }

    /// <summary>Produces a clone of the current <see cref="EmbeddingGenerationOptions"/> instance.</summary>
    /// <returns>A clone of the current <see cref="EmbeddingGenerationOptions"/> instance.</returns>
    /// <remarks>
    /// The clone will have the same values for all properties as the original instance. Any collections, like <see cref="AdditionalProperties"/>
    /// are shallow-cloned, meaning a new collection instance is created, but any references contained by the collections are shared with the original.
    /// </remarks>
    public virtual EmbeddingGenerationOptions Clone() =>
        new()
        {
            AdditionalProperties = AdditionalProperties?.Clone(),
            Dimensions = Dimensions,
            ModelId = ModelId,
            RawRepresentationFactory = RawRepresentationFactory,
        };

    /// <summary>Merges the options specified by <paramref name="other"/> into this instance.</summary>
    /// <param name="other">The other options to be merged into this instance.</param>
    /// <param name="overwrite">
    /// If <see langword="true"/>, properties on this instance will be overwritten by the values from <paramref name="other"/>;
    /// if <see langword="false"/>, properties on this instance will only be updated if they are <see langword="null"/> on this instance.
    /// The default is <see langword="false"/>.
    /// </param>
    /// <remarks>
    /// Merging works by copying the values from <paramref name="other"/> into this instance.
    /// <para>
    /// When <paramref name="overwrite"/> is <see langword="false"/> (the default):
    /// <list type="bullet">
    ///   <item>
    ///     <description>For properties of primitive types (like <see cref="Dimensions"/> or <see cref="ModelId"/>), 
    ///     properties on this instance will only be updated if they are <see langword="null"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>For dictionary types (like <see cref="AdditionalProperties"/>), a shallow copy is performed 
    ///     on the entries from <paramref name="other"/>, adding them into the corresponding dictionary on this instance, 
    ///     but only if the key does not already exist in this instance's dictionary.</description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// When <paramref name="overwrite"/> is <see langword="true"/>, any non-<see langword="null"/> properties on <paramref name="other"/> will
    /// overwrite the corresponding properties on this instance.
    /// <list type="bullet">
    ///   <item>
    ///     <description>For properties of primitive types (like <see cref="Dimensions"/> or <see cref="ModelId"/>), 
    ///     properties on this instance will be updated with values from <paramref name="other"/> if they're non-<see langword="null"/>
    ///     on <paramref name="other"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>For dictionary types (like <see cref="AdditionalProperties"/>), entries from <paramref name="other"/> 
    ///     will be stored into this instance's dictionary, overwriting any entries with the same key in this instance's dictionary.</description>
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
    public virtual void Merge(EmbeddingGenerationOptions? other, bool overwrite = false)
    {
        if (other is not null)
        {
            if (overwrite)
            {
                MergeOverwrite(other);
            }
            else
            {
                MergeDefault(other);
            }
        }
    }

    private void MergeDefault(EmbeddingGenerationOptions other)
    {
        if (other.AdditionalProperties is { Count: > 0 })
        {
            if (AdditionalProperties is null)
            {
                AdditionalProperties = other.AdditionalProperties.Clone();
            }
            else
            {
                foreach (var entry in other.AdditionalProperties)
                {
                    _ = AdditionalProperties.TryAdd(entry.Key, entry.Value);
                }
            }
        }

        Dimensions ??= other.Dimensions;

        ModelId ??= other.ModelId;

        if (other.RawRepresentationFactory is { } otherRrf)
        {
            RawRepresentationFactory = RawRepresentationFactory is { } originalRrf ?
                generator => originalRrf(generator) ?? otherRrf(generator) :
                otherRrf;
        }
    }

    private void MergeOverwrite(EmbeddingGenerationOptions other)
    {
        if (other.AdditionalProperties is { Count: > 0 })
        {
            if (AdditionalProperties is null)
            {
                AdditionalProperties = other.AdditionalProperties.Clone();
            }
            else
            {
                AdditionalProperties.SetAll(other.AdditionalProperties);
            }
        }

        if (other.Dimensions is not null)
        {
            Dimensions = other.Dimensions;
        }

        if (other.ModelId is not null)
        {
            ModelId = other.ModelId;
        }

        if (other.RawRepresentationFactory is not null)
        {
            RawRepresentationFactory = other.RawRepresentationFactory;
        }
    }
}
