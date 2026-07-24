using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace aichatweb.Web.Services;

#pragma warning disable MEAI001 // Mock provider uses experimental testing APIs

internal sealed class LexicalMockEmbeddingGenerator : MockEmbeddingGenerator<string>
{
    private readonly int _dimensions;

    internal LexicalMockEmbeddingGenerator(int dimensions)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(dimensions, 1);
        _dimensions = dimensions;
    }

    protected override Task<GeneratedEmbeddings<Embedding<float>>> GenerateCoreAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options,
        CancellationToken cancellationToken)
    {
        if (GenerateAsyncCallback is not null)
        {
            return base.GenerateCoreAsync(values, options, cancellationToken);
        }

        GeneratedEmbeddings<Embedding<float>> embeddings = [];
        foreach (string value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            embeddings.Add(new Embedding<float>(CreateVector(value, _dimensions)));
        }

        return Task.FromResult(embeddings);
    }

    private static float[] CreateVector(string? value, int dimensions)
    {
        var vector = new float[dimensions];
        if (string.IsNullOrEmpty(value))
        {
            return vector;
        }

        // Hash tokens and three-character shingles so shared words and word fragments have matching dimensions.
        int tokenStart = -1;
        for (int i = 0; i <= value.Length; i++)
        {
            if (i < value.Length && char.IsLetterOrDigit(value[i]))
            {
                tokenStart = tokenStart < 0 ? i : tokenStart;
            }
            else if (tokenStart >= 0)
            {
                AddTokenFeatures(value, tokenStart, i - tokenStart, vector);
                tokenStart = -1;
            }
        }

        // Normalize to unit length so cosine similarity favors lexical overlap rather than document length.
        float squaredMagnitude = 0;
        foreach (float component in vector)
        {
            squaredMagnitude += component * component;
        }

        if (squaredMagnitude > 0)
        {
            float scale = 1 / (float)Math.Sqrt(squaredMagnitude);
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] *= scale;
            }
        }

        return vector;
    }

    private static void AddTokenFeatures(string value, int tokenStart, int tokenLength, float[] vector)
    {
        AddFeature(value, tokenStart, tokenLength, vector);

        for (int i = tokenStart; i <= tokenStart + tokenLength - 3; i++)
        {
            AddFeature(value, i, 3, vector);
        }
    }

    private static void AddFeature(string value, int start, int length, float[] vector)
    {
        uint hash = 2_166_136_261;
        unchecked
        {
            for (int i = start; i < start + length; i++)
            {
                hash = (hash ^ char.ToLowerInvariant(value[i])) * 16_777_619;
            }
        }

        vector[(int)(hash % (uint)vector.Length)] += 1;
    }
}
#pragma warning restore MEAI001
