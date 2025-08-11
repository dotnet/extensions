// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public class OpenAIImageGeneratorIntegrationTests : ImageGeneratorIntegrationTests
{
    protected override IImageGenerator? CreateGenerator()
        => IntegrationTestHelpers.GetOpenAIClient()?
            .GetImageClient(TestRunnerConfiguration.Instance["OpenAI:ImageModel"] ?? "dall-e-3")
            .AsIImageGenerator();
}
