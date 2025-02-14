// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Specifies the <see cref="IChatClient"/> and the <see cref="IEvaluationTokenCounter"/> that should be used when
/// evaluation is performed using an AI model.
/// </summary>
/// <param name="chatClient">An <see cref="IChatClient"/> that can be used to communicate with an AI model.</param>
/// <param name="tokenCounter">
/// An <see cref="IEvaluationTokenCounter"/> that can be used to counts tokens present in evaluation prompts, or
/// <see langword="null"/> if the AI model / deployment being used does not impose an input token limit.
/// </param>
public sealed class ChatConfiguration(IChatClient chatClient, IEvaluationTokenCounter? tokenCounter = null)
{
    /// <summary>
    /// Gets an <see cref="IChatClient"/> that can be used to communicate with an AI model.
    /// </summary>
    public IChatClient ChatClient { get; } = chatClient;

    /// <summary>
    /// Gets an <see cref="IEvaluationTokenCounter"/> that can be used to counts tokens present in evaluation prompts.
    /// </summary>
    /// <remarks>
    /// <see cref="TokenCounter"/> can be set to <see langword="null"/> if the AI model / deployment being used does
    /// not impose an input token limit.
    /// </remarks>
    public IEvaluationTokenCounter? TokenCounter { get; } = tokenCounter;
}
