// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using Azure.Core;
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

            // Use Azure endpoint with /openai/v1 suffix
            var azureEndpointUri = new Uri(new Uri(endpoint), "/openai/v1");

            if (apiKey is not null)
            {
                var options = new OpenAIClientOptions { Endpoint = azureEndpointUri };
                return new OpenAIClient(new ApiKeyCredential(apiKey), options);
            }
            else
            {
                // Use Azure Identity authentication - get token and use as API key
                var tokenCredential = new DefaultAzureCredential();
                var token = tokenCredential.GetToken(
                    new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]),
                    default);
                var options = new OpenAIClientOptions { Endpoint = azureEndpointUri };
                return new OpenAIClient(new ApiKeyCredential(token.Token), options);
            }
        }
        else if (apiKey is not null)
        {
            return new OpenAIClient(apiKey);
        }

        return null;
    }
}
