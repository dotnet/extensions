// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Counts the number of tokens present in evaluation prompts that are to be sent to an AI model.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IEvaluator"/>s that rely on an AI model to perform evaluations can use
/// <see cref="IEvaluationTokenCounter"/> to ensure that the evaluation prompts they use do not exceed the specified
/// <see cref="InputTokenLimit"/>.
/// </para>
/// <para>
/// The token counting implementation can vary depending on the AI model that is used. Use the
/// <see cref="TokenizerExtensions.ToTokenCounter(ML.Tokenizers.Tokenizer, int)"/> extension method to get a
/// <see cref="IEvaluationTokenCounter"/> from the <see cref="ML.Tokenizers.Tokenizer"/> for a given AI model.
/// </para>
/// </remarks>
public interface IEvaluationTokenCounter
{
    /// <summary>
    /// Gets the input token limit for the AI model / deployment in use.
    /// </summary>
    int InputTokenLimit { get; }

    /// <summary>
    /// Counts the number of tokens present in the supplied <paramref name="content"/>.
    /// </summary>
    /// <param name="content">The string content that is to be counted.</param>
    /// <returns>The number of tokens present in the supplied <paramref name="content"/>.</returns>
    int CountTokens(string content);
}
