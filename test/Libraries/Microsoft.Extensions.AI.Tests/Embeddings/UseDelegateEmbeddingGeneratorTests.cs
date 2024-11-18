// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class UseDelegateEmbeddingGeneratorTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        using var generator = new TestEmbeddingGenerator();
        EmbeddingGeneratorBuilder<string, Embedding<float>> builder = new(generator);

        Assert.Throws<ArgumentNullException>("generateFunc", () =>
            builder.Use((Func<IEnumerable<string>, EmbeddingGenerationOptions?, IEmbeddingGenerator<string, Embedding<float>>, CancellationToken, Task<GeneratedEmbeddings<Embedding<float>>>>)null!));

        Assert.Throws<ArgumentNullException>("innerGenerator", () =>
            new AnonymousDelegatingEmbeddingGenerator<string, Embedding<float>>(
                null!, (values, options, innerGenerator, cancellationToken) => Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(Array.Empty<Embedding<float>>()))));

        Assert.Throws<ArgumentNullException>("generateFunc", () =>
            new AnonymousDelegatingEmbeddingGenerator<string, Embedding<float>>(generator, null!));
    }

    [Fact]
    public async Task GenerateFunc_ContextPropagated()
    {
        GeneratedEmbeddings<Embedding<float>> expectedEmbeddings = new();
        IList<string> expectedValues = ["hello"];
        EmbeddingGenerationOptions expectedOptions = new();
        using CancellationTokenSource expectedCts = new();
        AsyncLocal<int> asyncLocal = new();

        using IEmbeddingGenerator<string, Embedding<float>> innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (values, options, cancellationToken) =>
            {
                Assert.Same(expectedValues, values);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return Task.FromResult(expectedEmbeddings);
            },
        };

        using IEmbeddingGenerator<string, Embedding<float>> generator = new EmbeddingGeneratorBuilder<string, Embedding<float>>(innerGenerator)
            .Use(async (values, options, innerGenerator, cancellationToken) =>
            {
                Assert.Same(expectedValues, values);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                asyncLocal.Value = 42;
                var e = await innerGenerator.GenerateAsync(values, options, cancellationToken);
                e.Add(new Embedding<float>(default));
                return e;
            })
            .Build();

        Assert.Equal(0, asyncLocal.Value);

        GeneratedEmbeddings<Embedding<float>> actual = await generator.GenerateAsync(expectedValues, expectedOptions, expectedCts.Token);
        Assert.Same(expectedEmbeddings, actual);
        Assert.Single(actual);
    }
}
