// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Azure;
using Azure.AI.Inference;

namespace Microsoft.Extensions.AI;

/// <summary>Shared utility methods for integration tests.</summary>
internal static class IntegrationTestHelpers
{
    /// <summary>Gets an <see cref="ChatCompletionsClient"/> to use for testing, or null if the associated tests should be disabled.</summary>
    public static ChatCompletionsClient? GetChatCompletionsClient()
    {
        string? apiKey =
            Environment.GetEnvironmentVariable("AZURE_AI_INFERENCE_APIKEY") ??
            Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (apiKey is not null)
        {
            string? endpoint =
                Environment.GetEnvironmentVariable("AZURE_AI_INFERENCE_ENDPOINT") ??
                "https://api.openai.com/v1";

            return new(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }

        return null;
    }
}
