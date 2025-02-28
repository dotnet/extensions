// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public class OpenAIChatClientIntegrationTests : ChatClientIntegrationTests
{
    protected override IChatClient? CreateChatClient() =>
        IntegrationTestHelpers.GetOpenAIClient()
        ?.AsChatClient(TestRunnerConfiguration.Instance["OpenAI:ChatModel"] ?? "gpt-4o-mini");
}
