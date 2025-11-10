// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>
/// Azure AI Inference-specific integration tests for ImageGeneratingChatClient.
/// Tests the ImageGeneratingChatClient with Azure AI Inference chat client implementation.
/// </summary>
public class AzureAIInferenceImageGeneratingChatClientIntegrationTests : ImageGeneratingChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient() =>
        IntegrationTestHelpers.GetChatCompletionsClient()
        ?.AsIChatClient(TestRunnerConfiguration.Instance["AzureAIInference:ChatModel"] ?? "gpt-4o-mini");
}
