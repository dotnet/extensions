// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Azure;
using Azure.AI.Inference;

namespace Microsoft.Extensions.AI;

/// <summary>Shared utility methods for integration tests.</summary>
internal static class IntegrationTestHelpers
{
    private static readonly string? _apiKey =
        TestRunnerConfiguration.Instance["AzureAIInference:Key"] ??
        TestRunnerConfiguration.Instance["OpenAI:Key"];

    private static readonly string _endpoint =
        TestRunnerConfiguration.Instance["AzureAIInference:Endpoint"] ??
        "https://api.openai.com/v1";

    /// <summary>Gets a <see cref="ChatResponsesClient"/> to use for testing, or null if the associated tests should be disabled.</summary>
    public static ChatCompletionsClient? GetChatCompletionsClient() =>
        _apiKey is string apiKey ?
            new ChatCompletionsClient(new Uri(_endpoint), new AzureKeyCredential(apiKey)) :
            null;

    /// <summary>Gets an <see cref="EmbeddingsClient"/> to use for testing, or null if the associated tests should be disabled.</summary>
    public static EmbeddingsClient? GetEmbeddingsClient() =>
        _apiKey is string apiKey ?
            new EmbeddingsClient(new Uri(_endpoint), new AzureKeyCredential(apiKey)) :
            null;
}
