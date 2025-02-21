// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public class OpenAIAudioTranscriptionClientIntegrationTests : AudioTranscriptionClientIntegrationTests
{
    protected override ISpeechToTextClient? CreateClient()
        => IntegrationTestHelpers.GetOpenAIClient()
        ?.AsAudioTranscriptionClient(TestRunnerConfiguration.Instance["OpenAI:AudioTranscriptionModel"] ?? "whisper-1");
}
