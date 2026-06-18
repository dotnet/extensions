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
    public static IEmbeddingGenerator<TextContent, TEmbedding> AsTextContentEmbeddingGenerator<TEmbedding>(
        this IEmbeddingGenerator<string, TEmbedding> stringGenerator)
        where TEmbedding : Embedding
    {
        _ = Shared.Diagnostics.Throw.IfNull(stringGenerator);

        return new TextContentEmbeddingGeneratorAdapter<TEmbedding>(stringGenerator);
    }

    private sealed class TextContentEmbeddingGeneratorAdapter<TEmbedding> : IEmbeddingGenerator<TextContent, TEmbedding>
        where TEmbedding : Embedding
    {
        private readonly IEmbeddingGenerator<string, TEmbedding> _innerGenerator;

        internal TextContentEmbeddingGeneratorAdapter(IEmbeddingGenerator<string, TEmbedding> innerGenerator)
        {
            _innerGenerator = innerGenerator;
        }

        public Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
            IEnumerable<TextContent> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            _ = Shared.Diagnostics.Throw.IfNull(values);

            IEnumerable<string> stringValues = values.Select(content => content.Text);
            return _innerGenerator.GenerateAsync(stringValues, options, cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            _ = Shared.Diagnostics.Throw.IfNull(serviceType);

            return serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
                _innerGenerator.GetService(serviceType, serviceKey);
        }

        public void Dispose()
            => _innerGenerator.Dispose();
    }
}
