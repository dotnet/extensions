// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Inference;
using Azure.Core;
using Azure.Core.Pipeline;

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

    /// <summary>Gets a <see cref="ChatResponsesClient"/> to use for testing, or <see langword="null"/> if the associated tests should be disabled.</summary>
    public static ChatCompletionsClient? GetChatCompletionsClient() =>
        _apiKey is string apiKey ?
            new ChatCompletionsClient(new Uri(_endpoint), new AzureKeyCredential(apiKey), CreateOptions()) :
            null;

    /// <summary>Gets an <see cref="EmbeddingsClient"/> to use for testing, or <see langword="null"/> if the associated tests should be disabled.</summary>
    public static EmbeddingsClient? GetEmbeddingsClient() =>
        _apiKey is string apiKey ?
            new EmbeddingsClient(new Uri(_endpoint), new AzureKeyCredential(apiKey), CreateOptions()) :
            null;

    private static AzureAIInferenceClientOptions CreateOptions()
    {
        var result = new AzureAIInferenceClientOptions();

        // The API vesion set here corresponds to the value used by AzureOpenAIClientOptions
        // if the AZURE_OPENAI_GA flag is set during its compilation. This API version is the
        // minimum required for structured output with JSON schema.
        result.AddPolicy(new OverrideApiVersionPolicy("2024-08-01-preview"), HttpPipelinePosition.PerCall);

        return result;
    }

    // From https://github.com/Azure/azure-sdk-for-net/issues/48405#issuecomment-2704360548
    private class OverrideApiVersionPolicy : HttpPipelinePolicy
    {
        private string ApiVersion { get; }

        public OverrideApiVersionPolicy(string apiVersion)
        {
            ApiVersion = apiVersion;
        }

        public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            message.Request.Uri.Query = $"?api-version={ApiVersion}";
            ProcessNext(message, pipeline);
        }

        public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            message.Request.Uri.Query = $"?api-version={ApiVersion}";
            var task = ProcessNextAsync(message, pipeline);

            return task;
        }
    }
}
