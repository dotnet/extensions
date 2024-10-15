// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>Represents an embedding composed of a vector of <typeparamref name="T"/> values.</summary>
/// <typeparam name="T">The type of the values in the embedding vector.</typeparam>
/// <remarks>Typical values of <typeparamref name="T"/> are <see cref="float"/>, <see cref="double"/>, or Half.</remarks>
public sealed class Embedding<T> : Embedding
{
    /// <summary>Initializes a new instance of the <see cref="Embedding{T}"/> class with the embedding vector.</summary>
    /// <param name="vector">The embedding vector this embedding represents.</param>
    public Embedding(ReadOnlyMemory<T> vector)
    {
        Vector = vector;
    }

    /// <summary>Gets or sets the embedding vector this embedding represents.</summary>
    public ReadOnlyMemory<T> Vector { get; set; }
}
