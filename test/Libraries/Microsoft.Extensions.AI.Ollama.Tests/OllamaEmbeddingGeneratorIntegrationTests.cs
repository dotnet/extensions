// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

public class OllamaEmbeddingGeneratorIntegrationTests : EmbeddingGeneratorIntegrationTests
{
    protected override IEmbeddingGenerator<string, Embedding<float>>? CreateEmbeddingGenerator() =>
        IntegrationTestHelpers.GetOllamaUri() is Uri endpoint ?
            new OllamaEmbeddingGenerator(endpoint, "all-minilm") :
            null;
}
