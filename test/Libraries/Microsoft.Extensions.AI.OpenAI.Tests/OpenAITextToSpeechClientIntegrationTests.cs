// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public class OpenAITextToSpeechClientIntegrationTests : TextToSpeechClientIntegrationTests
{
    protected override ITextToSpeechClient? CreateClient()
        => IntegrationTestHelpers.GetOpenAIClient()?
            .GetAudioClient(TestRunnerConfiguration.Instance["OpenAI:TextToSpeechModel"] ?? "tts-1")
            .AsITextToSpeechClient();
}
