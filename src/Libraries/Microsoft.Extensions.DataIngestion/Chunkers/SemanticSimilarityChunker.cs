// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion.Chunkers;

/// <summary>
/// Splits a <see cref="IngestionDocument"/> into chunks based on semantic similarity between its elements.
/// </summary>
public sealed class SemanticSimilarityChunker : IngestionChunker<string>
{
    private readonly ElementsChunker _elementsChunker;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly float _thresholdPercentile;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSimilarityChunker"/> class.
    /// </summary>
    /// <param name="embeddingGenerator">Embedding generator.</param>
    /// <param name="options">The options for the chunker.</param>
    /// <param name="thresholdPercentile">Threshold percentile to consider the chunks to be sufficiently similar.</param>
    public SemanticSimilarityChunker(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IngestionChunkerOptions options,
        float thresholdPercentile = 95.0f)
    {
        _embeddingGenerator = Throw.IfNull(embeddingGenerator);
        _elementsChunker = new(options);

        if (thresholdPercentile < 0f || thresholdPercentile > 100f)
        {
            Throw.ArgumentOutOfRangeException(nameof(thresholdPercentile), "Threshold percentile must be between 0 and 100.");
        }

        _thresholdPercentile = thresholdPercentile;
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IngestionDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {

        _ = Throw.IfNull(document);

        if (document.Sections.Count == 0)
        {
            yield break;
        }

        List<(IngestionDocumentElement, float)> distances = await CalculateDistancesAsync(document, cancellationToken).ConfigureAwait(false);
        foreach (var chunk in MakeChunks(document, distances))
        {
            yield return chunk;
        }
    }

    private async Task<List<(IngestionDocumentElement element, float distance)>> CalculateDistancesAsync(IngestionDocument documents, CancellationToken cancellationToken)
    {
        List<(IngestionDocumentElement element, float distance)> elementDistance = [];
        List<string> semanticContents = [];

        foreach (IngestionDocumentElement element in documents.EnumerateContent())
        {
            string? semanticContent = element is IngestionDocumentImage img
                ? img.AlternativeText ?? img.Text
                : element.GetMarkdown();

            if (!string.IsNullOrEmpty(semanticContent))
            {
                elementDistance.Add((element, default));
                semanticContents.Add(semanticContent!);
            }
        }

        var embeddings = await _embeddingGenerator.GenerateAsync(semanticContents, cancellationToken: cancellationToken).ConfigureAwait(false);

        for (int i = 0; i < elementDistance.Count - 1; i++)
        {
            float distance = 1 - TensorPrimitives.CosineSimilarity(embeddings[i].Vector.Span, embeddings[i + 1].Vector.Span);
            elementDistance[i] = (elementDistance[i].element, distance);
        }

        return elementDistance;
    }

    private IEnumerable<IngestionChunk<string>> MakeChunks(IngestionDocument document, List<(IngestionDocumentElement element, float distance)> elementDistances)
    {
        float distanceThreshold = Percentile(elementDistances);

        List<IngestionDocumentElement> elementAccumulator = [];
        string context = string.Empty; // we could implement some simple heuristic
        foreach (var (element, distance) in elementDistances)
        {
            elementAccumulator.Add(element);
            if (distance > distanceThreshold)
            {
                foreach (var chunk in _elementsChunker.Process(document, context, elementAccumulator))
                {
                    yield return chunk;
                }
                elementAccumulator.Clear();
            }
        }

        if (elementAccumulator.Count > 0)
        {
            foreach (var chunk in _elementsChunker.Process(document, context, elementAccumulator))
            {
                yield return chunk;
            }
        }
    }

    private float Percentile(List<(IngestionDocumentElement element, float distance)> elementDistances)
    {
        if (elementDistances.Count == 0)
        {
            return 0f;
        }
        else if (elementDistances.Count == 1)
        {
            return elementDistances[0].distance;
        }

        float[] sorted = new float[elementDistances.Count];
        for (int elementIndex = 0; elementIndex < elementDistances.Count; elementIndex++)
        {
            sorted[elementIndex] = elementDistances[elementIndex].distance;
        }
        Array.Sort(sorted);

        float i = (_thresholdPercentile / 100f) * (sorted.Length - 1);
        int i0 = (int)i;
        int i1 = Math.Min(i0 + 1, sorted.Length - 1);
        return sorted[i0] + ((i - i0) * (sorted[i1] - sorted[i0]));
    }
}
