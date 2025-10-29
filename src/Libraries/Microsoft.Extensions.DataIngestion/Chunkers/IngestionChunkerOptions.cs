// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.ML.Tokenizers;
using System;

namespace Microsoft.Extensions.DataIngestion;

public class IngestionChunkerOptions
{
    // Default values come from https://learn.microsoft.com/en-us/azure/search/vector-search-how-to-chunk-documents#text-split-skill-example
    private int _maxTokensPerChunk = 2_000;
    private const int DefaultOverlapTokens = 500;
    private int? _overlapTokens;

    public IngestionChunkerOptions(Tokenizer tokenizer)
    {
        Tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
    }

    public Tokenizer Tokenizer { get; }

    /// <summary>
    /// The maximum number of tokens allowed in each chunk. Default is 2000.
    /// </summary>
    public int MaxTokensPerChunk
    {
        get => _maxTokensPerChunk;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Chunk size must be greater than zero.");
            }
            else if (_overlapTokens.HasValue && value <= _overlapTokens.Value)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Chunk size must be greater than chunk overlap.");
            }

            _maxTokensPerChunk = value;
        }
    }
    /// <summary>
    /// The number of overlapping tokens between consecutive chunks. Default is 500.
    /// </summary>
    public int OverlapTokens
    {
        get
        {
            if (_overlapTokens.HasValue)
            {
                return _overlapTokens.Value;
            }
            else if (_maxTokensPerChunk > DefaultOverlapTokens)
            {
                return DefaultOverlapTokens;
            }
            else
            {
                return 0;
            }
        }
        set => _overlapTokens = value < 0
            ? throw new ArgumentOutOfRangeException(nameof(value))
            : value >= _maxTokensPerChunk
                ? throw new ArgumentOutOfRangeException(nameof(value), "Chunk overlap must be less than chunk size.")
                : value;
    }

    /// <summary>
    /// Indicate whether to consider pre-tokenization before tokenization.
    /// </summary>
    internal bool ConsiderPreTokenization { get; set; } = false;

    /// <summary>
    /// Indicate whether to consider normalization before tokenization.
    /// </summary>
    internal bool ConsiderNormalization { get; set; } = false;
}
