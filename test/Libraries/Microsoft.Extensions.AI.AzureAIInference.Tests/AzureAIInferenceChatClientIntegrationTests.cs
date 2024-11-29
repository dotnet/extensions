// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public class AzureAIInferenceChatClientIntegrationTests : ChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient() =>
        IntegrationTestHelpers.GetChatCompletionsClient()
            ?.AsChatClient(TestRunnerConfiguration.Instance["AzureAIInference:ChatModel"] ?? "gpt-4o-mini");
}
