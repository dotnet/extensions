// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// A tool reduction strategy that ranks tools by embedding similarity to the current conversation context.
/// </summary>
/// <remarks>
/// The strategy embeds each tool (name + description by default) once (cached) and embeds the current
/// conversation content each request. It then selects the top <c>toolLimit</c> tools by similarity.
/// </remarks>
[Experimental("MEAI001")]
public sealed class EmbeddingToolReductionStrategy : IToolReductionStrategy
{
    private readonly ConditionalWeakTable<AITool, Embedding<float>> _toolEmbeddingsCache = new();
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly int _toolLimit;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingToolReductionStrategy"/> class.
    /// </summary>
    /// <param name="embeddingGenerator">Embedding generator used to produce embeddings.</param>
    /// <param name="toolLimit">Maximum number of tools to return. Must be greater than zero.</param>
    public EmbeddingToolReductionStrategy(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        int toolLimit)
    {
        _embeddingGenerator = Throw.IfNull(embeddingGenerator);
        _toolLimit = Throw.IfLessThanOrEqual(toolLimit, min: 0);
    }

    /// <summary>
    /// Gets or sets a delegate used to produce the text to embed for a tool.
    /// Defaults to: <c>Name + "\n" + Description</c> (omitting empty parts).
    /// </summary>
    public Func<AITool, string> EmbeddingTextFactory
    {
        get => field ??= static t =>
        {
            if (string.IsNullOrWhiteSpace(t.Name))
            {
                return t.Description;
            }

            if (string.IsNullOrWhiteSpace(t.Description))
            {
                return t.Name;
            }

            return t.Name + "\n" + t.Description;
        };
        set => field = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets a similarity function applied to (query, tool) embedding vectors. Defaults to cosine similarity.
    /// </summary>
    public Func<ReadOnlyMemory<float>, ReadOnlyMemory<float>, float> Similarity
    {
        get => field ??= static (a, b) => TensorPrimitives.CosineSimilarity(a.Span, b.Span);
        set => field = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether tool embeddings are cached. Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableEmbeddingCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to preserve original ordering of selected tools.
    /// If <see langword="false"/> (default), tools are ordered by descending similarity.
    /// If <see langword="true"/>, the top-N tools by similarity are re-emitted in their original order.
    /// </summary>
    public bool PreserveOriginalOrdering { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of most recent messages to include when forming the query embedding.
    /// Defaults to <see cref="int.MaxValue"/> (all messages).
    /// </summary>
    public int MaxMessagesForQueryEmbedding { get; set; } = int.MaxValue;

    /// <inheritdoc />
    public async Task<IEnumerable<AITool>> SelectToolsForRequestAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        if (options?.Tools is not { Count: > 0 } tools)
        {
            return options?.Tools ?? [];
        }

        Debug.Assert(_toolLimit > 0, "Expected the tool count limit to be greater than zero.");

        if (tools.Count <= _toolLimit)
        {
            // No reduction necessary.
            return tools;
        }

        // Build query text from recent messages.
        var messageTexts = messages.Select(m => m.Text).Where(s => !string.IsNullOrEmpty(s));
        var queryText = string.Join("\n", messageTexts);
        if (string.IsNullOrWhiteSpace(queryText))
        {
            // We couldn't build a meaningful query, likely because the message list was empty.
            // We'll just return a truncated list of tools.
            return tools.Take(_toolLimit);
        }

        // Ensure embeddings for any uncached tools are generated in a batch.
        var toolEmbeddings = await GetToolEmbeddingsAsync(tools, cancellationToken).ConfigureAwait(false);

        // Generate the query embedding.
        var queryEmbedding = await _embeddingGenerator.GenerateAsync(queryText, cancellationToken: cancellationToken).ConfigureAwait(false);
        var queryVector = queryEmbedding.Vector;

        // Compute rankings.
        var ranked = tools
            .Zip(toolEmbeddings, static (tool, embedding) => (Tool: tool, Embedding: embedding))
            .Select((t, i) => (t.Tool, Index: i, Score: Similarity(queryVector, t.Embedding.Vector)))
            .OrderByDescending(t => t.Score)
            .Take(_toolLimit);

        if (PreserveOriginalOrdering)
        {
            ranked = ranked.OrderBy(t => t.Index);
        }

        return ranked.Select(t => t.Tool);
    }

    private async Task<IReadOnlyList<Embedding<float>>> GetToolEmbeddingsAsync(IList<AITool> tools, CancellationToken cancellationToken)
    {
        if (!EnableEmbeddingCaching)
        {
            // Embed all tools in one batch; do not store in cache.
            return await ComputeEmbeddingsAsync(tools.Select(t => EmbeddingTextFactory(t)), expectedCount: tools.Count);
        }

        var result = new Embedding<float>[tools.Count];
        var cacheMisses = new List<(AITool Tool, int Index)>(tools.Count);

        for (var i = 0; i < tools.Count; i++)
        {
            if (_toolEmbeddingsCache.TryGetValue(tools[i], out var embedding))
            {
                result[i] = embedding;
            }
            else
            {
                cacheMisses.Add((tools[i], i));
            }
        }

        if (cacheMisses.Count == 0)
        {
            return result;
        }

        var uncachedEmbeddings = await ComputeEmbeddingsAsync(cacheMisses.Select(t => EmbeddingTextFactory(t.Tool)), expectedCount: cacheMisses.Count);

        for (var i = 0; i < cacheMisses.Count; i++)
        {
            var embedding = uncachedEmbeddings[i];
            result[cacheMisses[i].Index] = embedding;
            _toolEmbeddingsCache.Add(cacheMisses[i].Tool, embedding);
        }

        return result;

        async ValueTask<GeneratedEmbeddings<Embedding<float>>> ComputeEmbeddingsAsync(IEnumerable<string> texts, int expectedCount)
        {
            var embeddings = await _embeddingGenerator.GenerateAsync(texts, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (embeddings.Count != expectedCount)
            {
                Throw.InvalidOperationException($"Expected {expectedCount} embeddings, got {embeddings.Count}.");
            }

            return embeddings;
        }
    }
}
