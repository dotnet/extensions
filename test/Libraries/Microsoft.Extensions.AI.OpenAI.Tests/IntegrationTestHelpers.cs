// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Identity;
using OpenAI;

#pragma warning disable OPENAI001 // Experimental OpenAI APIs

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
            var options = new OpenAIClientOptions { Endpoint = new Uri(new Uri(endpoint), "/openai/v1") };
            return apiKey is not null ?
                new OpenAIClient(new ApiKeyCredential(apiKey), options) :
                new OpenAIClient(
                    new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
                    options);
        }
        else if (apiKey is not null)
        {
            return new OpenAIClient(apiKey);
        }

        return null;
    }
}
