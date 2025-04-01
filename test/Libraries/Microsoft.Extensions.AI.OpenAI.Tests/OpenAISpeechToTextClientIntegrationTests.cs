// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public class OpenAISpeechToTextClientIntegrationTests : SpeechToTextClientIntegrationTests
{
    protected override ISpeechToTextClient? CreateClient()
    {
        var openAIClient = IntegrationTestHelpers.GetOpenAIClient();

        if (openAIClient is null)
        {
            return null;
        }

        return new OpenAISpeechToTextClient(
            openAIClient: openAIClient,
            modelId: TestRunnerConfiguration.Instance["OpenAI:AudioTranscriptionModel"] ?? "whisper-1");
    }
}
