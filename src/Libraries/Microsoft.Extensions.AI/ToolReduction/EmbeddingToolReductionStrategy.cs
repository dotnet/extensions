// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

#pragma warning disable IDE0032 // Use auto property, suppressed until repo updates to C# 14

/// <summary>
/// A tool reduction strategy that ranks tools by embedding similarity to the current conversation context.
/// </summary>
/// <remarks>
/// The strategy embeds each tool (name + description by default) once (cached) and embeds the current
/// conversation content each request. It then selects the top <c>toolLimit</c> tools by similarity.
/// </remarks>
[Experimental(diagnosticId: DiagnosticIds.Experiments.ToolReduction, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class EmbeddingToolReductionStrategy : IToolReductionStrategy
{
    private readonly ConditionalWeakTable<AITool, Embedding<float>> _toolEmbeddingsCache = new();
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly int _toolLimit;

    private Func<AITool, string> _toolEmbeddingTextSelector = static t =>
    {
        if (string.IsNullOrWhiteSpace(t.Name))
        {
            return t.Description;
        }

        if (string.IsNullOrWhiteSpace(t.Description))
        {
            return t.Name;
        }

        return t.Name + Environment.NewLine + t.Description;
    };

    private Func<IEnumerable<ChatMessage>, ValueTask<string>> _messagesEmbeddingTextSelector = static messages =>
    {
        var sb = new StringBuilder();
        foreach (var message in messages)
        {
            var contents = message.Contents;
            for (var i = 0; i < contents.Count; i++)
            {
                string text;
                switch (contents[i])
                {
                    case TextContent content:
                        text = content.Text;
                        break;
                    case TextReasoningContent content:
                        text = content.Text;
                        break;
                    default:
                        continue;
                }

                _ = sb.AppendLine(text);
            }
        }

        return new ValueTask<string>(sb.ToString());
    };

    private Func<ReadOnlyMemory<float>, ReadOnlyMemory<float>, float> _similarity = static (a, b) => TensorPrimitives.CosineSimilarity(a.Span, b.Span);

    private Func<AITool, bool> _isRequiredTool = static _ => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingToolReductionStrategy"/> class.
    /// </summary>
    /// <param name="embeddingGenerator">Embedding generator used to produce embeddings.</param>
    /// <param name="toolLimit">Maximum number of tools to return, excluding required tools. Must be greater than zero.</param>
    public EmbeddingToolReductionStrategy(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        int toolLimit)
    {
        _embeddingGenerator = Throw.IfNull(embeddingGenerator);
        _toolLimit = Throw.IfLessThanOrEqual(toolLimit, min: 0);
    }

    /// <summary>
    /// Gets or sets the selector used to generate a single text string from a tool.
    /// </summary>
    /// <remarks>
    /// Defaults to: <c>Name + "\n" + Description</c> (omitting empty parts).
    /// </remarks>
    public Func<AITool, string> ToolEmbeddingTextSelector
    {
        get => _toolEmbeddingTextSelector;
        set => _toolEmbeddingTextSelector = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets the selector used to generate a single text string from a collection of chat messages for
    /// embedding purposes.
    /// </summary>
    public Func<IEnumerable<ChatMessage>, ValueTask<string>> MessagesEmbeddingTextSelector
    {
        get => _messagesEmbeddingTextSelector;
        set => _messagesEmbeddingTextSelector = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets a similarity function applied to (query, tool) embedding vectors.
    /// </summary>
    /// <remarks>
    /// Defaults to cosine similarity.
    /// </remarks>
    public Func<ReadOnlyMemory<float>, ReadOnlyMemory<float>, float> Similarity
    {
        get => _similarity;
        set => _similarity = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets a function that determines whether a tool is required (always included).
    /// </summary>
    /// <remarks>
    /// If this returns <see langword="true"/>, the tool is included regardless of ranking and does not count against
    /// the configured non-required tool limit. A tool explicitly named by <see cref="RequiredChatToolMode"/> (when
    /// <see cref="RequiredChatToolMode.RequiredFunctionName"/> is non-null) is also treated as required, independent
    /// of this delegate's result.
    /// </remarks>
    public Func<AITool, bool> IsRequiredTool
    {
        get => _isRequiredTool;
        set => _isRequiredTool = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to preserve original ordering of selected tools.
    /// If <see langword="false"/> (default), tools are ordered by descending similarity.
    /// If <see langword="true"/>, the top-N tools by similarity are re-emitted in their original order.
    /// </summary>
    public bool PreserveOriginalOrdering { get; set; }

    /// <inheritdoc />
    public async Task<IEnumerable<AITool>> SelectToolsForRequestAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        if (options?.Tools is not { Count: > 0 } tools)
        {
            // Prefer the original tools list reference if possible.
            // This allows ToolReducingChatClient to avoid unnecessarily copying ChatOptions.
            // When no reduction is performed.
            return options?.Tools ?? [];
        }

        Debug.Assert(_toolLimit > 0, "Expected the tool count limit to be greater than zero.");

        if (tools.Count <= _toolLimit)
        {
            // Since the total number of tools doesn't exceed the configured tool limit,
            // there's no need to determine which tools are optional, i.e., subject to reduction.
            // We can return the original tools list early.
            return tools;
        }

        var toolRankingInfoArray = ArrayPool<AIToolRankingInfo>.Shared.Rent(tools.Count);
        try
        {
            var toolRankingInfoMemory = toolRankingInfoArray.AsMemory(start: 0, length: tools.Count);

            // We allocate tool rankings in a contiguous chunk of memory, but partition them such that
            // required tools come first and are immediately followed by optional tools.
            // This allows us to separately rank optional tools by similarity score, but then later re-order
            // the top N tools (including required tools) to preserve their original relative order.
            var (requiredTools, optionalTools) = PartitionToolRankings(toolRankingInfoMemory, tools, options.ToolMode);

            if (optionalTools.Length <= _toolLimit)
            {
                // There aren't enough optional tools to require reduction, so we'll return the original
                // tools list.
                return tools;
            }

            // Build query text from recent messages.
            var queryText = await MessagesEmbeddingTextSelector(messages).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(queryText))
            {
                // We couldn't build a meaningful query, likely because the message list was empty.
                // We'll just return the original tools list.
                return tools;
            }

            var queryEmbedding = await _embeddingGenerator.GenerateAsync(queryText, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Compute and populate similarity scores in the tool ranking info.
            await ComputeSimilarityScoresAsync(optionalTools, queryEmbedding, cancellationToken);

            var topTools = toolRankingInfoMemory.Slice(start: 0, length: requiredTools.Length + _toolLimit);
#if NET
            optionalTools.Span.Sort(AIToolRankingInfo.CompareByDescendingSimilarityScore);
            if (PreserveOriginalOrdering)
            {
                topTools.Span.Sort(AIToolRankingInfo.CompareByOriginalIndex);
            }
#else
            Array.Sort(toolRankingInfoArray, index: requiredTools.Length, length: optionalTools.Length, AIToolRankingInfo.CompareByDescendingSimilarityScore);
            if (PreserveOriginalOrdering)
            {
                Array.Sort(toolRankingInfoArray, index: 0, length: topTools.Length, AIToolRankingInfo.CompareByOriginalIndex);
            }
#endif
            return ToToolList(topTools.Span);

            static List<AITool> ToToolList(ReadOnlySpan<AIToolRankingInfo> toolInfo)
            {
                var result = new List<AITool>(capacity: toolInfo.Length);
                foreach (var info in toolInfo)
                {
                    result.Add(info.Tool);
                }

                return result;
            }
        }
        finally
        {
            ArrayPool<AIToolRankingInfo>.Shared.Return(toolRankingInfoArray);
        }
    }

    private (Memory<AIToolRankingInfo> RequiredTools, Memory<AIToolRankingInfo> OptionalTools) PartitionToolRankings(
        Memory<AIToolRankingInfo> toolRankingInfo, IList<AITool> tools, ChatToolMode? toolMode)
    {
        // Always include a tool if its name matches the required function name.
        var requiredFunctionName = (toolMode as RequiredChatToolMode)?.RequiredFunctionName;
        var nextRequiredToolIndex = 0;
        var nextOptionalToolIndex = tools.Count - 1;
        for (var i = 0; i < toolRankingInfo.Length; i++)
        {
            var tool = tools[i];
            var isRequiredByToolMode = requiredFunctionName is not null && string.Equals(requiredFunctionName, tool.Name, StringComparison.Ordinal);
            var toolIndex = isRequiredByToolMode || IsRequiredTool(tool)
                ? nextRequiredToolIndex++
                : nextOptionalToolIndex--;
            toolRankingInfo.Span[toolIndex] = new AIToolRankingInfo(tool, originalIndex: i);
        }

        return (
            RequiredTools: toolRankingInfo.Slice(0, nextRequiredToolIndex),
            OptionalTools: toolRankingInfo.Slice(nextRequiredToolIndex));
    }

    private async Task ComputeSimilarityScoresAsync(Memory<AIToolRankingInfo> toolInfo, Embedding<float> queryEmbedding, CancellationToken cancellationToken)
    {
        var anyCacheMisses = false;
        List<string> cacheMissToolEmbeddingTexts = null!;
        List<int> cacheMissToolInfoIndexes = null!;
        for (var i = 0; i < toolInfo.Length; i++)
        {
            ref var info = ref toolInfo.Span[i];
            if (_toolEmbeddingsCache.TryGetValue(info.Tool, out var toolEmbedding))
            {
                info.SimilarityScore = Similarity(queryEmbedding.Vector, toolEmbedding.Vector);
            }
            else
            {
                if (!anyCacheMisses)
                {
                    anyCacheMisses = true;
                    cacheMissToolEmbeddingTexts = [];
                    cacheMissToolInfoIndexes = [];
                }

                var text = ToolEmbeddingTextSelector(info.Tool);
                cacheMissToolEmbeddingTexts.Add(text);
                cacheMissToolInfoIndexes.Add(i);
            }
        }

        if (!anyCacheMisses)
        {
            // There were no cache misses; no more work to do.
            return;
        }

        var uncachedEmbeddings = await _embeddingGenerator.GenerateAsync(cacheMissToolEmbeddingTexts, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (uncachedEmbeddings.Count != cacheMissToolEmbeddingTexts.Count)
        {
            throw new InvalidOperationException($"Expected {cacheMissToolEmbeddingTexts.Count} embeddings, got {uncachedEmbeddings.Count}.");
        }

        for (var i = 0; i < uncachedEmbeddings.Count; i++)
        {
            var toolInfoIndex = cacheMissToolInfoIndexes[i];
            var toolEmbedding = uncachedEmbeddings[i];
            ref var info = ref toolInfo.Span[toolInfoIndex];
            info.SimilarityScore = Similarity(queryEmbedding.Vector, toolEmbedding.Vector);
            _toolEmbeddingsCache.Add(info.Tool, toolEmbedding);
        }
    }

    private struct AIToolRankingInfo(AITool tool, int originalIndex)
    {
        public static readonly Comparer<AIToolRankingInfo> CompareByDescendingSimilarityScore
            = Comparer<AIToolRankingInfo>.Create(static (a, b) =>
            {
                var result = b.SimilarityScore.CompareTo(a.SimilarityScore);
                return result != 0
                    ? result
                    : a.OriginalIndex.CompareTo(b.OriginalIndex); // Stabilize ties.
            });

        public static readonly Comparer<AIToolRankingInfo> CompareByOriginalIndex
            = Comparer<AIToolRankingInfo>.Create(static (a, b) => a.OriginalIndex.CompareTo(b.OriginalIndex));

        public AITool Tool { get; } = tool;
        public int OriginalIndex { get; } = originalIndex;
        public float SimilarityScore { get; set; }
    }
}
