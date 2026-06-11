// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Provides extension methods for adapting embedding generators between <see cref="string"/> and <see cref="AIContent"/> input types.
/// </summary>
public static class EmbeddingGeneratorExtensions
{
    /// <summary>
    /// Wraps an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that accepts <see cref="string"/> inputs
    /// into one that accepts <see cref="AIContent"/> inputs, extracting text from <see cref="TextContent"/> instances.
    /// </summary>
    /// <typeparam name="TEmbedding">The type of the embedding produced by the generator.</typeparam>
    /// <param name="stringGenerator">The string-based embedding generator to wrap.</param>
    /// <returns>An <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that accepts <see cref="AIContent"/> inputs.</returns>
    /// <remarks>
    /// The returned generator only supports <see cref="TextContent"/> instances. Passing any other
    /// <see cref="AIContent"/>-derived type will throw a <see cref="NotSupportedException"/>.
    /// </remarks>
    public static IEmbeddingGenerator<AIContent, TEmbedding> AsAIContentEmbeddingGenerator<TEmbedding>(
        this IEmbeddingGenerator<string, TEmbedding> stringGenerator)
        where TEmbedding : Embedding
    {
        _ = Shared.Diagnostics.Throw.IfNull(stringGenerator);

        return new AIContentEmbeddingGeneratorAdapter<TEmbedding>(stringGenerator);
    }

    private sealed class AIContentEmbeddingGeneratorAdapter<TEmbedding> : IEmbeddingGenerator<AIContent, TEmbedding>
        where TEmbedding : Embedding
    {
        private readonly IEmbeddingGenerator<string, TEmbedding> _innerGenerator;

        internal AIContentEmbeddingGeneratorAdapter(IEmbeddingGenerator<string, TEmbedding> innerGenerator)
        {
            _innerGenerator = innerGenerator;
        }

        public Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
            IEnumerable<AIContent> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<string> stringValues = values.Select(content =>
            {
                if (content is TextContent tc)
                {
                    return tc.Text;
                }

                throw new NotSupportedException(
                    $"The embedding generator only supports TextContent inputs, but received '{content.GetType().Name}'.");
            });

            return _innerGenerator.GenerateAsync(stringValues, options, cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => _innerGenerator.GetService(serviceType, serviceKey);

        public void Dispose()
            => _innerGenerator.Dispose();
    }
}
