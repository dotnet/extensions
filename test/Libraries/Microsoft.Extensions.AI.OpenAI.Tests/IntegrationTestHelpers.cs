// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Identity;
using OpenAI;

namespace Microsoft.Extensions.AI;

/// <summary>Shared utility methods for integration tests.</summary>
internal static class IntegrationTestHelpers
{
    /// <summary>Gets an <see cref="OpenAIClient"/> to use for testing, or <see langword="null"/> if the associated tests should be disabled.</summary>
    public static OpenAIClient? GetOpenAIClient()
    {
        var configuration = TestRunnerConfiguration.Instance;

        string? apiKey = configuration["OpenAI:Key"];
        string? mode = configuration["OpenAI:Mode"];

        if (string.Equals(mode, "AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = configuration["OpenAI:Endpoint"]
                ?? throw new InvalidOperationException("To use AzureOpenAI, set a value for OpenAI:Endpoint");

            if (apiKey is not null)
            {
                return new OpenAIClient(
                    new ApiKeyCredential(apiKey), 
                    new OpenAIClientOptions { Endpoint = new Uri(endpoint.TrimEnd('/') + "/openai/v1") });
            }
            else
            {
                return new OpenAIClient(
                    new BearerTokenPolicy(new DefaultAzureCredential(), "https://cognitiveservices.azure.com/.default"), 
                    new OpenAIClientOptions { Endpoint = new Uri(endpoint.TrimEnd('/') + "/openai/v1") });
            }
        }
        else if (apiKey is not null)
        {
            return new OpenAIClient(apiKey);
        }

        return null;
    }
}
