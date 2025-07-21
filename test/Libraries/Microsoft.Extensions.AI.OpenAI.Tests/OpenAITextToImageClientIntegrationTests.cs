// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public class OpenAITextToImageClientIntegrationTests : TextToImageClientIntegrationTests
{
    protected override ITextToImageClient? CreateClient()
        => IntegrationTestHelpers.GetOpenAIClient()?
            .AsITextToImageClient(TestRunnerConfiguration.Instance["OpenAI:ImageModel"] ?? "dall-e-3");
}
