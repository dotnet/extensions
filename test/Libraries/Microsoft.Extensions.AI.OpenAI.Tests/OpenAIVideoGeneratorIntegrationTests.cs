// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable OPENAI001 // Experimental OpenAI APIs

namespace Microsoft.Extensions.AI;

public class OpenAIVideoGeneratorIntegrationTests : VideoGeneratorIntegrationTests
{
    protected override IVideoGenerator? CreateGenerator()
        => IntegrationTestHelpers.GetOpenAIClient()?
            .GetVideoClient()
            .AsIVideoGenerator(TestRunnerConfiguration.Instance["OpenAI:VideoModel"] ?? "sora");
}
