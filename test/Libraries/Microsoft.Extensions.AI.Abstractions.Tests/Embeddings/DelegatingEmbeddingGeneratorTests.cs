// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingEmbeddingGeneratorTests
{
    [Fact]
    public void RequiresInnerService()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new NoOpDelegatingEmbeddingGenerator(null!));
    }

    [Fact]
    public async Task GenerateEmbeddingsDefaultsToInnerServiceAsync()
    {
        // Arrange
        var expectedInput = new List<string>();
        using var cts = new CancellationTokenSource();
        var expectedCancellationToken = cts.Token;
        var expectedResult = new TaskCompletionSource<GeneratedEmbeddings<Embedding<float>>>();
        var expectedEmbedding = new GeneratedEmbeddings<Embedding<float>>([new(new float[] { 1.0f, 2.0f, 3.0f })]);
        using var inner = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (input, options, cancellationToken) =>
            {
                Assert.Same(expectedInput, input);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingEmbeddingGenerator(inner);

        // Act
        var resultTask = delegating.GenerateAsync(expectedInput, options: null, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedEmbedding);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedEmbedding, await resultTask);
    }

    [Fact]
    public void GetServiceThrowsForNullType()
    {
        using var inner = new TestEmbeddingGenerator();
        using var delegating = new NoOpDelegatingEmbeddingGenerator(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        // Arrange
        using var inner = new TestEmbeddingGenerator();
        using var delegating = new NoOpDelegatingEmbeddingGenerator(inner);

        // Act
        var service = delegating.GetService<DelegatingEmbeddingGenerator<string, Embedding<float>>>();

        // Assert
        Assert.Same(delegating, service);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        // Arrange
        var expectedParam = new object();
        var expectedKey = new object();
        using var expectedResult = new TestEmbeddingGenerator();
        using var inner = new TestEmbeddingGenerator
        {
            GetServiceCallback = (_, _) => expectedResult
        };
        using var delegating = new NoOpDelegatingEmbeddingGenerator(inner);

        // Act
        var service = delegating.GetService<IEmbeddingGenerator<string, Embedding<float>>>(expectedKey);

        // Assert
        Assert.Same(expectedResult, service);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfNotCompatibleWithRequest()
    {
        // Arrange
        var expectedParam = new object();
        var expectedResult = TimeZoneInfo.Local;
        var expectedKey = new object();
        using var inner = new TestEmbeddingGenerator
        {
            GetServiceCallback = (type, key) => type == expectedResult.GetType() && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingEmbeddingGenerator(inner);

        // Act
        var service = delegating.GetService<TimeZoneInfo>(expectedKey);

        // Assert
        Assert.Same(expectedResult, service);
    }

    private sealed class NoOpDelegatingEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator) :
        DelegatingEmbeddingGenerator<string, Embedding<float>>(innerGenerator);
}
