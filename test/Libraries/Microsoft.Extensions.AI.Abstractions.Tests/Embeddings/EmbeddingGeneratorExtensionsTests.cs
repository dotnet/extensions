// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class EmbeddingGeneratorExtensionsTests
{
    [Fact]
    public async Task GenerateAsync_InvalidArgs_ThrowsAsync()
    {
        await Assert.ThrowsAsync<ArgumentNullException>("generator", () => ((TestEmbeddingGenerator)null!).GenerateAsync("hello"));
    }

    [Fact]
    public async Task GenerateAsync_ReturnsSingleEmbeddingAsync()
    {
        Embedding<float> result = new(new float[] { 1f, 2f, 3f });

        using TestEmbeddingGenerator service = new()
        {
            GenerateAsyncCallback = (values, options, cancellationToken) =>
                Task.FromResult<GeneratedEmbeddings<Embedding<float>>>([result])
        };

        Assert.Same(result, (await service.GenerateAsync("hello"))[0]);
    }
}
