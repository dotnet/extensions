// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a generator of embeddings.</summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
/// <remarks>
/// <para>
/// Unless otherwise specified, all members of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> are thread-safe for concurrent use.
/// It is expected that all implementations of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> support being used by multiple requests concurrently.
/// Instances must not be disposed of while the instance is still in use.
/// </para>
/// <para>
/// However, implementations of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> may mutate the arguments supplied to
/// <see cref="GenerateAsync"/>, such as by configuring the options instance. Thus, consumers of the interface either should
/// avoid using shared instances of these arguments for concurrent invocations or should otherwise ensure by construction that
/// no <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> instances are used which might employ such mutation.
/// </para>
/// </remarks>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#the-iembeddinggenerator-interface">The IEmbeddingGenerator interface.</related>
public interface IEmbeddingGenerator<in TInput, TEmbedding> : IEmbeddingGenerator
    where TEmbedding : Embedding
{
    /// <summary>Generates embeddings for each of the supplied <paramref name="values"/>.</summary>
    /// <param name="values">The sequence of values for which to generate embeddings.</param>
    /// <param name="options">The embedding generation options with which to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The generated embeddings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#create-embeddings">Create embeddings.</related>
    Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
}
