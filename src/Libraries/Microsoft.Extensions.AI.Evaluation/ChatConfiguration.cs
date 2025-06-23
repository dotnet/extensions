// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

namespace Microsoft.Extensions.AI.Evaluation;

/// <summary>
/// Specifies the <see cref="IChatClient"/> that should be used when evaluation is performed using an AI model.
/// </summary>
/// <param name="chatClient">An <see cref="IChatClient"/> that can be used to communicate with an AI model.</param>
public sealed class ChatConfiguration(IChatClient chatClient)
{
    /// <summary>
    /// Gets an <see cref="IChatClient"/> that can be used to communicate with an AI model.
    /// </summary>
    public IChatClient ChatClient { get; } = chatClient;
}
