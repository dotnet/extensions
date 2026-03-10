// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using OllamaSharp;

namespace Microsoft.Extensions.AI;

/// <summary>
/// OllamaSharp-specific integration tests for ImageGeneratingChatClient.
/// Tests the ImageGeneratingChatClient with OllamaSharp chat client implementation.
/// </summary>
public class OllamaSharpImageGeneratingChatClientIntegrationTests : ImageGeneratingChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient() =>
        IntegrationTestHelpers.GetOllamaUri() is Uri endpoint ?
            new OllamaApiClient(endpoint, "llama3.2") :
            null;
}
