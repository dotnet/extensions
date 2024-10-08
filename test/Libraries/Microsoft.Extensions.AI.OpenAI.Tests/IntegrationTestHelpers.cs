// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using Azure.AI.OpenAI;
using OpenAI;

namespace Microsoft.Extensions.AI;

/// <summary>Shared utility methods for integration tests.</summary>
internal static class IntegrationTestHelpers
{
    /// <summary>Gets an <see cref="OpenAIClient"/> to use for testing, or null if the associated tests should be disabled.</summary>
    public static OpenAIClient? GetOpenAIClient()
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (apiKey is not null)
        {
            if (string.Equals(Environment.GetEnvironmentVariable("OPENAI_MODE"), "AzureOpenAI", StringComparison.OrdinalIgnoreCase))
            {
                var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT")
                    ?? throw new InvalidOperationException("To use AzureOpenAI, set a value for OPENAI_ENDPOINT");
                return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
            }
            else
            {
                return new OpenAIClient(apiKey);
            }
        }

        return null;
    }
}
