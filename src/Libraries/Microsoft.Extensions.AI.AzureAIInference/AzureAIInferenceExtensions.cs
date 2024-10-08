// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.AI.Inference;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with Azure AI Inference.</summary>
public static class AzureAIInferenceExtensions
{
    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="ChatCompletionsClient"/>.</summary>
    /// <param name="chatCompletionsClient">The client.</param>
    /// <param name="modelId">The id of the model to use. If null, it may be provided per request via <see cref="ChatOptions.ModelId"/>.</param>
    /// <returns>An <see cref="IChatClient"/> that may be used to converse via the <see cref="ChatCompletionsClient"/>.</returns>
    public static IChatClient AsChatClient(this ChatCompletionsClient chatCompletionsClient, string? modelId = null) =>
        new AzureAIInferenceChatClient(chatCompletionsClient, modelId);
}
