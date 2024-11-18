// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DistributedCachingEmbeddingGeneratorTest
{
    private readonly TestInMemoryCacheStorage _storage = new();
    private readonly Embedding<float> _expectedEmbedding = new(new float[] { 1.0f, 2.0f, 3.0f })
    {
        CreatedAt = DateTimeOffset.Parse("2024-08-01T00:00:00Z"),
        ModelId = "someModel",
        AdditionalProperties = new() { ["a"] = "b" },
    };

    [Fact]
    public async Task CachesSuccessResultsAsync()
    {
        // Arrange

        // Verify that all the expected properties will round-trip through the cache,
        // even if this involves serialization
        var innerCallCount = 0;
        using var testGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (values, options, cancellationToken) =>
            {
                innerCallCount++;
                return Task.FromResult<GeneratedEmbeddings<Embedding<float>>>([_expectedEmbedding]);
            },
        };
        using var outer = new DistributedCachingEmbeddingGenerator<string, Embedding<float>>(testGenerator, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options,
        };

        // Make the initial request and do a quick sanity check
        var result1 = await outer.GenerateEmbeddingAsync("abc");
        AssertEmbeddingsEqual(_expectedEmbedding, result1);
        Assert.Equal(1, innerCallCount);

        // Act
        var result2 = await outer.GenerateEmbeddingAsync("abc");

        // Assert
        Assert.Equal(1, innerCallCount);
        AssertEmbeddingsEqual(_expectedEmbedding, result2);

        // Act/Assert 2: Cache misses do not return cached results
        await outer.GenerateAsync(["def"]);
        Assert.Equal(2, innerCallCount);
    }

    [Fact]
    public async Task SupportsPartiallyCachedBatchesAsync()
    {
        // Arrange

        // Verify that all the expected properties will round-trip through the cache,
        // even if this involves serialization
        var innerCallCount = 0;
        Embedding<float>[] expected = Enumerable.Range(0, 10).Select(i =>
            new Embedding<float>(new[] { 1.0f, 2.0f, 3.0f })
            {
                CreatedAt = DateTimeOffset.Parse("2024-08-01T00:00:00Z") + TimeSpan.FromHours(i),
                ModelId = $"someModel{i}",
                AdditionalProperties = new() { [$"a{i}"] = $"b{i}" },
            }).ToArray();
        using var testGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (values, options, cancellationToken) =>
            {
                innerCallCount++;
                Assert.Equal(innerCallCount == 1 ? 4 : 6, values.Count());
                return Task.FromResult<GeneratedEmbeddings<Embedding<float>>>(new(values.Select(i => expected[int.Parse(i)])));
            },
        };
        using var outer = new DistributedCachingEmbeddingGenerator<string, Embedding<float>>(testGenerator, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options,
        };

        // Make initial requests for some of the values
        var results = await outer.GenerateAsync(["0", "4", "5", "8"]);
        Assert.Equal(1, innerCallCount);
        Assert.Equal(4, results.Count);
        AssertEmbeddingsEqual(expected[0], results[0]);
        AssertEmbeddingsEqual(expected[4], results[1]);
        AssertEmbeddingsEqual(expected[5], results[2]);
        AssertEmbeddingsEqual(expected[8], results[3]);

        // Act/Assert
        results = await outer.GenerateAsync(["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"]);
        Assert.Equal(2, innerCallCount);
        for (int i = 0; i < 10; i++)
        {
            AssertEmbeddingsEqual(expected[i], results[i]);
        }

        results = await outer.GenerateAsync(["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"]);
        Assert.Equal(2, innerCallCount);
        for (int i = 0; i < 10; i++)
        {
            AssertEmbeddingsEqual(expected[i], results[i]);
        }
    }

    [Fact]
    public async Task AllowsConcurrentCallsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        var completionTcs = new TaskCompletionSource<bool>();
        using var innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = async (value, options, cancellationToken) =>
            {
                innerCallCount++;
                await completionTcs.Task;
                return [_expectedEmbedding];
            }
        };
        using var outer = new DistributedCachingEmbeddingGenerator<string, Embedding<float>>(innerGenerator, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options,
        };

        // Act 1: Concurrent calls before resolution are passed into the inner client
        var result1 = outer.GenerateEmbeddingAsync("abc");
        var result2 = outer.GenerateEmbeddingAsync("abc");

        // Assert 1
        Assert.Equal(2, innerCallCount);
        Assert.False(result1.IsCompleted);
        Assert.False(result2.IsCompleted);
        completionTcs.SetResult(true);
        AssertEmbeddingsEqual(_expectedEmbedding, await result1);
        AssertEmbeddingsEqual(_expectedEmbedding, await result2);

        // Act 2: Subsequent calls after completion are resolved from the cache
        var result3 = await outer.GenerateEmbeddingAsync("abc");
        Assert.Equal(2, innerCallCount);
        AssertEmbeddingsEqual(_expectedEmbedding, await result1);
    }

    [Fact]
    public async Task DoesNotCacheExceptionResultsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        using var innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (value, options, cancellationToken) =>
            {
                innerCallCount++;
                throw new InvalidTimeZoneException("some failure");
            }
        };
        using var outer = new DistributedCachingEmbeddingGenerator<string, Embedding<float>>(innerGenerator, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options,
        };

        var ex1 = await Assert.ThrowsAsync<InvalidTimeZoneException>(() => outer.GenerateEmbeddingAsync("abc"));
        Assert.Equal("some failure", ex1.Message);
        Assert.Equal(1, innerCallCount);

        // Act
        var ex2 = await Assert.ThrowsAsync<InvalidTimeZoneException>(() => outer.GenerateEmbeddingAsync("abc"));

        // Assert
        Assert.NotSame(ex1, ex2);
        Assert.Equal("some failure", ex2.Message);
        Assert.Equal(2, innerCallCount);
    }

    [Fact]
    public async Task DoesNotCacheCanceledResultsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        var resolutionTcs = new TaskCompletionSource<bool>();
        using var innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = async (value, options, cancellationToken) =>
            {
                innerCallCount++;
                if (innerCallCount == 1)
                {
                    await resolutionTcs.Task;
                }

                return [_expectedEmbedding];
            }
        };
        using var outer = new DistributedCachingEmbeddingGenerator<string, Embedding<float>>(innerGenerator, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options,
        };

        // First call gets cancelled
        var result1 = outer.GenerateEmbeddingAsync("abc");
        Assert.False(result1.IsCompleted);
        Assert.Equal(1, innerCallCount);
        resolutionTcs.SetCanceled();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => result1);
        Assert.True(result1.IsCanceled);

        // Act/Assert: Second call can succeed
        var result2 = await outer.GenerateEmbeddingAsync("abc");
        Assert.Equal(2, innerCallCount);
        AssertEmbeddingsEqual(_expectedEmbedding, result2);
    }

    [Fact]
    public async Task CacheKeyVariesByEmbeddingOptionsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        var completionTcs = new TaskCompletionSource<bool>();
        using var innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = async (value, options, cancellationToken) =>
            {
                innerCallCount++;
                await Task.Yield();
                return [new(((string)options!.AdditionalProperties!["someKey"]!).Select(c => (float)c).ToArray())];
            }
        };
        using var outer = new DistributedCachingEmbeddingGenerator<string, Embedding<float>>(innerGenerator, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options,
        };

        // Act: Call with two different EmbeddingGenerationOptions that have the same values
        var result1 = await outer.GenerateEmbeddingAsync("abc", new EmbeddingGenerationOptions
        {
            AdditionalProperties = new() { ["someKey"] = "value 1" }
        });
        var result2 = await outer.GenerateEmbeddingAsync("abc", new EmbeddingGenerationOptions
        {
            AdditionalProperties = new() { ["someKey"] = "value 1" }
        });

        // Assert: Same result
        Assert.Equal(1, innerCallCount);
        AssertEmbeddingsEqual(new("value 1".Select(c => (float)c).ToArray()), result1);
        AssertEmbeddingsEqual(new("value 1".Select(c => (float)c).ToArray()), result2);

        // Act: Call with two different EmbeddingGenerationOptions that have different values
        var result3 = await outer.GenerateEmbeddingAsync("abc", new EmbeddingGenerationOptions
        {
            AdditionalProperties = new() { ["someKey"] = "value 1" }
        });
        var result4 = await outer.GenerateEmbeddingAsync("abc", new EmbeddingGenerationOptions
        {
            AdditionalProperties = new() { ["someKey"] = "value 2" }
        });

        // Assert: Different result
        Assert.Equal(2, innerCallCount);
        AssertEmbeddingsEqual(new("value 1".Select(c => (float)c).ToArray()), result3);
        AssertEmbeddingsEqual(new("value 2".Select(c => (float)c).ToArray()), result4);
    }

    [Fact]
    public async Task SubclassCanOverrideCacheKeyToVaryByOptionsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        var completionTcs = new TaskCompletionSource<bool>();
        using var innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = async (value, options, cancellationToken) =>
            {
                innerCallCount++;
                await Task.Yield();
                return [_expectedEmbedding];
            }
        };
        using var outer = new CachingEmbeddingGeneratorWithCustomKey(innerGenerator, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options,
        };

        // Act: Call with two different options
        var result1 = await outer.GenerateEmbeddingAsync("abc", new EmbeddingGenerationOptions
        {
            AdditionalProperties = new() { ["someKey"] = "value 1" }
        });
        var result2 = await outer.GenerateEmbeddingAsync("abc", new EmbeddingGenerationOptions
        {
            AdditionalProperties = new() { ["someKey"] = "value 2" }
        });

        // Assert: Different results
        Assert.Equal(2, innerCallCount);
        AssertEmbeddingsEqual(_expectedEmbedding, result1);
        AssertEmbeddingsEqual(_expectedEmbedding, result2);
    }

    [Fact]
    public async Task CanResolveIDistributedCacheFromDI()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton<IDistributedCache>(_storage)
            .BuildServiceProvider();
        using var testGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = (values, options, cancellationToken) =>
            {
                return Task.FromResult<GeneratedEmbeddings<Embedding<float>>>([_expectedEmbedding]);
            },
        };
        using var outer = testGenerator
            .AsBuilder()
            .UseDistributedCache(configure: instance =>
            {
                instance.JsonSerializerOptions = TestJsonSerializerContext.Default.Options;
            })
            .Build(services);

        // Act: Make a request that should populate the cache
        Assert.Empty(_storage.Keys);
        var result = await outer.GenerateEmbeddingAsync("abc");

        // Assert
        Assert.NotNull(result);
        Assert.Single(_storage.Keys);
    }

    private static void AssertEmbeddingsEqual(Embedding<float> expected, Embedding<float> actual)
    {
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.ModelId, actual.ModelId);
        Assert.Equal(expected.Vector.ToArray(), actual.Vector.ToArray());
        Assert.Equal(
            JsonSerializer.Serialize(expected.AdditionalProperties, TestJsonSerializerContext.Default.Options),
            JsonSerializer.Serialize(actual.AdditionalProperties, TestJsonSerializerContext.Default.Options));
    }

    private sealed class CachingEmbeddingGeneratorWithCustomKey(IEmbeddingGenerator<string, Embedding<float>> innerGenerator, IDistributedCache storage)
        : DistributedCachingEmbeddingGenerator<string, Embedding<float>>(innerGenerator, storage)
    {
        protected override string GetCacheKey(string value, EmbeddingGenerationOptions? options) =>
            base.GetCacheKey(value, options) + options?.AdditionalProperties?["someKey"]?.ToString();
    }
}
