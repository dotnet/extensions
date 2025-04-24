// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public class OpenAISpeechToTextClientIntegrationTests : SpeechToTextClientIntegrationTests
{
    protected override ISpeechToTextClient? CreateClient()
        => IntegrationTestHelpers.GetOpenAIClient()?
            .GetAudioClient(TestRunnerConfiguration.Instance["OpenAI:AudioTranscriptionModel"] ?? "whisper-1")
            .AsISpeechToTextClient();
}
