﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using OllamaSharp;

namespace Microsoft.Extensions.AI;

public class OllamaSharpEmbeddingGeneratorIntegrationTests : OllamaEmbeddingGeneratorIntegrationTests
{
    protected override IEmbeddingGenerator<string, Embedding<float>>? CreateEmbeddingGenerator() =>
        IntegrationTestHelpers.GetOllamaUri() is Uri endpoint ?
            new OllamaApiClient(endpoint, "all-minilm") :
            null;
}
