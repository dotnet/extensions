// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using Microsoft.ML.Tokenizers;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Extension methods for <see cref="Tokenizer"/>.
/// </summary>
public static class TokenizerExtensions
{
    private sealed class TokenCounter(Tokenizer tokenizer, int inputTokenLimit) : IEvaluationTokenCounter
    {
        public int InputTokenLimit { get; } = inputTokenLimit;

        public int CountTokens(string content)
            => tokenizer.CountTokens(content);
    }

    /// <summary>
    /// Returns an <see cref="IEvaluationTokenCounter"/> given the <see cref="Tokenizer"/> and the
    /// <paramref name="inputTokenLimit"/> for a particular AI model / deployment.
    /// </summary>
    /// <param name="tokenizer">The <see cref="Tokenizer"/> for a particular AI model.</param>
    /// <param name="inputTokenLimit">
    /// The threshold of maximum allowed input tokens for a particular AI model / deployment.
    /// </param>
    /// <returns>
    /// An <see cref="IEvaluationTokenCounter"/> for a particular AI model / deployment.
    /// </returns>
    public static IEvaluationTokenCounter ToTokenCounter(this Tokenizer tokenizer, int inputTokenLimit)
    {
        _ = Throw.IfNull(tokenizer, nameof(tokenizer));

        return new TokenCounter(tokenizer, inputTokenLimit);
    }
}
