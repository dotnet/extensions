// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DistributedCachingChatClientTest
{
    private readonly TestInMemoryCacheStorage _storage = new();

    [Fact]
    public void Ctor_ExpectedDefaults()
    {
        using var innerClient = new TestChatClient();
        using var cachingClient = new DistributedCachingChatClient(innerClient, _storage);

        Assert.True(cachingClient.CoalesceStreamingUpdates);

        cachingClient.CoalesceStreamingUpdates = false;
        Assert.False(cachingClient.CoalesceStreamingUpdates);

        cachingClient.CoalesceStreamingUpdates = true;
        Assert.True(cachingClient.CoalesceStreamingUpdates);
    }

    [Fact]
    public async Task CachesSuccessResultsAsync()
    {
        // Arrange

        // Verify that all the expected properties will round-trip through the cache,
        // even if this involves serialization
        var expectedCompletion = new ChatCompletion([
            new(new ChatRole("fakeRole"), "This is some content")
            {
                AdditionalProperties = new() { ["a"] = "b" },
                Contents = [new FunctionCallContent("someCallId", "functionName", new Dictionary<string, object?>
                {
                    ["arg1"] = "value1",
                    ["arg2"] = 123,
                    ["arg3"] = 123.4,
                    ["arg4"] = true,
                    ["arg5"] = false,
                    ["arg6"] = null
                })]
            }
        ])
        {
            CompletionId = "someId",
            Usage = new()
            {
                InputTokenCount = 123,
                OutputTokenCount = 456,
                TotalTokenCount = 99999,
                AdditionalValues = new()
                {
                    { "intVal", 1 },
                    { "longVal", 2L },
                    { "floatVal", 3.4f },
                    { "doubleVal", 5.6 },
                    { "decimalVal", 6.7m },
                }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            ModelId = "someModel",
            AdditionalProperties = new() { ["key1"] = "value1", ["key2"] = 123 }
        };

        var innerCallCount = 0;
        using var testClient = new TestChatClient
        {
            CompleteAsyncCallback = delegate
            {
                innerCallCount++;
                return Task.FromResult(expectedCompletion);
            }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        // Make the initial request and do a quick sanity check
        var result1 = await outer.CompleteAsync([new ChatMessage(ChatRole.User, "some input")]);
        Assert.Same(expectedCompletion, result1);
        Assert.Equal(1, innerCallCount);

        // Act
        var result2 = await outer.CompleteAsync([new ChatMessage(ChatRole.User, "some input")]);

        // Assert
        Assert.Equal(1, innerCallCount);
        AssertCompletionsEqual(expectedCompletion, result2);

        // Act/Assert 2: Cache misses do not return cached results
        await outer.CompleteAsync([new ChatMessage(ChatRole.User, "some modified input")]);
        Assert.Equal(2, innerCallCount);
    }

    [Fact]
    public async Task AllowsConcurrentCallsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        var completionTcs = new TaskCompletionSource<bool>();
        using var testClient = new TestChatClient
        {
            CompleteAsyncCallback = async delegate
            {
                innerCallCount++;
                await completionTcs.Task;
                return new ChatCompletion([new(ChatRole.Assistant, "Hello")]);
            }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        // Act 1: Concurrent calls before resolution are passed into the inner client
        var result1 = outer.CompleteAsync([new ChatMessage(ChatRole.User, "some input")]);
        var result2 = outer.CompleteAsync([new ChatMessage(ChatRole.User, "some input")]);

        // Assert 1
        Assert.Equal(2, innerCallCount);
        Assert.False(result1.IsCompleted);
        Assert.False(result2.IsCompleted);
        completionTcs.SetResult(true);
        Assert.Equal("Hello", (await result1).Message.Text);
        Assert.Equal("Hello", (await result2).Message.Text);

        // Act 2: Subsequent calls after completion are resolved from the cache
        var result3 = outer.CompleteAsync([new ChatMessage(ChatRole.User, "some input")]);
        Assert.Equal(2, innerCallCount);
        Assert.Equal("Hello", (await result3).Message.Text);
    }

    [Fact]
    public async Task DoesNotCacheExceptionResultsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        using var testClient = new TestChatClient
        {
            CompleteAsyncCallback = delegate
            {
                innerCallCount++;
                throw new InvalidTimeZoneException("some failure");
            }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        var input = new ChatMessage(ChatRole.User, "abc");
        var ex1 = await Assert.ThrowsAsync<InvalidTimeZoneException>(() => outer.CompleteAsync([input]));
        Assert.Equal("some failure", ex1.Message);
        Assert.Equal(1, innerCallCount);

        // Act
        var ex2 = await Assert.ThrowsAsync<InvalidTimeZoneException>(() => outer.CompleteAsync([input]));

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
        using var testClient = new TestChatClient
        {
            CompleteAsyncCallback = async delegate
            {
                innerCallCount++;
                if (innerCallCount == 1)
                {
                    await resolutionTcs.Task;
                }

                return new ChatCompletion([new(ChatRole.Assistant, "A good result")]);
            }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        // First call gets cancelled
        var input = new ChatMessage(ChatRole.User, "abc");
        var result1 = outer.CompleteAsync([input]);
        Assert.False(result1.IsCompleted);
        Assert.Equal(1, innerCallCount);
        resolutionTcs.SetCanceled();
        await Assert.ThrowsAsync<TaskCanceledException>(() => result1);
        Assert.True(result1.IsCanceled);

        // Act/Assert: Second call can succeed
        var result2 = await outer.CompleteAsync([input]);
        Assert.Equal(2, innerCallCount);
        Assert.Equal("A good result", result2.Message.Text);
    }

    [Fact]
    public async Task StreamingCachesSuccessResultsAsync()
    {
        // Arrange

        // Verify that all the expected properties will round-trip through the cache,
        // even if this involves serialization
        List<StreamingChatCompletionUpdate> actualCompletion =
        [
            new()
            {
                Role = new ChatRole("fakeRole1"),
                ChoiceIndex = 1,
                AdditionalProperties = new() { ["a"] = "b" },
                Contents = [new TextContent("Chunk1")]
            },
            new()
            {
                Role = new ChatRole("fakeRole2"),
                Contents =
                [
                    new FunctionCallContent("someCallId", "someFn", new Dictionary<string, object?> { ["arg1"] = "value1" }),
                    new UsageContent(new() { InputTokenCount = 123, OutputTokenCount = 456, TotalTokenCount = 99999 }),
                ]
            }
        ];

        List<StreamingChatCompletionUpdate> expectedCachedCompletion =
        [
            new()
            {
                Role = new ChatRole("fakeRole2"),
                Contents = [new FunctionCallContent("someCallId", "someFn", new Dictionary<string, object?> { ["arg1"] = "value1" })],
            },
            new()
            {
                Role = new ChatRole("fakeRole1"),
                ChoiceIndex = 1,
                AdditionalProperties = new() { ["a"] = "b" },
                Contents = [new TextContent("Chunk1")]
            },
            new()
            {
                Contents = [new UsageContent(new() { InputTokenCount = 123, OutputTokenCount = 456, TotalTokenCount = 99999 })],
            },
        ];

        var innerCallCount = 0;
        using var testClient = new TestChatClient
        {
            CompleteStreamingAsyncCallback = delegate
            {
                innerCallCount++;
                return ToAsyncEnumerableAsync(actualCompletion);
            }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        // Make the initial request and do a quick sanity check
        var result1 = outer.CompleteStreamingAsync([new ChatMessage(ChatRole.User, "some input")]);
        await AssertCompletionsEqualAsync(actualCompletion, result1);
        Assert.Equal(1, innerCallCount);

        // Act
        var result2 = outer.CompleteStreamingAsync([new ChatMessage(ChatRole.User, "some input")]);

        // Assert
        Assert.Equal(1, innerCallCount);
        await AssertCompletionsEqualAsync(expectedCachedCompletion, result2);

        // Act/Assert 2: Cache misses do not return cached results
        await ToListAsync(outer.CompleteStreamingAsync([new ChatMessage(ChatRole.User, "some modified input")]));
        Assert.Equal(2, innerCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [InlineData(null)]
    public async Task StreamingCoalescesConsecutiveTextChunksAsync(bool? coalesce)
    {
        // Arrange
        List<StreamingChatCompletionUpdate> expectedCompletion =
        [
            new() { Role = ChatRole.Assistant, Text = "This" },
            new() { Role = ChatRole.Assistant, Text = " becomes one chunk" },
            new() { Role = ChatRole.Assistant, Contents = [new FunctionCallContent("callId1", "separator")] },
            new() { Role = ChatRole.Assistant, Text = "... and this" },
            new() { Role = ChatRole.Assistant, Text = " becomes another" },
            new() { Role = ChatRole.Assistant, Text = " one." },
        ];

        using var testClient = new TestChatClient
        {
            CompleteStreamingAsyncCallback = delegate { return ToAsyncEnumerableAsync(expectedCompletion); }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        if (coalesce is not null)
        {
            outer.CoalesceStreamingUpdates = coalesce.Value;
        }

        var result1 = outer.CompleteStreamingAsync([new ChatMessage(ChatRole.User, "some input")]);
        await ToListAsync(result1);

        // Act
        var result2 = outer.CompleteStreamingAsync([new ChatMessage(ChatRole.User, "some input")]);

        // Assert
        if (coalesce is null or true)
        {
            StreamingChatCompletionUpdate update = Assert.Single(await ToListAsync(result2));
            Assert.Collection(update.Contents,
                c => Assert.Equal("This becomes one chunk", Assert.IsType<TextContent>(c).Text),
                c => Assert.IsType<FunctionCallContent>(c),
                c => Assert.Equal("... and this becomes another one.", Assert.IsType<TextContent>(c).Text));
        }
        else
        {
            Assert.Collection(await ToListAsync(result2),
                c => Assert.Equal("This", c.Text),
                c => Assert.Equal(" becomes one chunk", c.Text),
                c => Assert.IsType<FunctionCallContent>(Assert.Single(c.Contents)),
                c => Assert.Equal("... and this", c.Text),
                c => Assert.Equal(" becomes another", c.Text),
                c => Assert.Equal(" one.", c.Text));
        }
    }

    [Fact]
    public async Task StreamingCoalescingPropagatesMetadataAsync()
    {
        // Arrange
        List<StreamingChatCompletionUpdate> expectedCompletion =
        [
            new() { Role = ChatRole.Assistant, Contents = [new TextContent("Hello")] },
            new() { Role = ChatRole.Assistant, Contents = [new TextContent(" world, ")] },
            new()
            {
                Role = ChatRole.Assistant,
                Contents =
                [
                    new TextContent("how ")
                    {
                        AdditionalProperties = new() { ["a"] = "b", ["c"] = "d" },
                    }
                ]
            },
            new()
            {
                Role = ChatRole.Assistant,
                Contents =
                [
                    new TextContent("are you?")
                    {
                        AdditionalProperties = new() { ["e"] = "f", ["g"] = "h" },
                    }
                ],
                CreatedAt = DateTime.Parse("2024-10-11T19:23:36.0152137Z"),
                CompletionId = "12345",
                AuthorName = "Someone",
                FinishReason = ChatFinishReason.Length,
            },
        ];

        using var testClient = new TestChatClient
        {
            CompleteStreamingAsyncCallback = delegate { return ToAsyncEnumerableAsync(expectedCompletion); }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        var result1 = outer.CompleteStreamingAsync([new ChatMessage(ChatRole.User, "some input")]);
        await ToListAsync(result1);

        // Act
        var result2 = outer.CompleteStreamingAsync([new ChatMessage(ChatRole.User, "some input")]);

        // Assert
        var items = await ToListAsync(result2);
        var item = Assert.Single(items);
        Assert.Equal("Hello world, how are you?", item.Text);
        Assert.Equal("12345", item.CompletionId);
        Assert.Equal("Someone", item.AuthorName);
        Assert.Equal(ChatFinishReason.Length, item.FinishReason);
        Assert.Equal(DateTime.Parse("2024-10-11T19:23:36.0152137Z"), item.CreatedAt);

        var content = Assert.IsType<TextContent>(Assert.Single(item.Contents));
        Assert.Equal("Hello world, how are you?", content.Text);
    }

    [Fact]
    public async Task StreamingAllowsConcurrentCallsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        var completionTcs = new TaskCompletionSource<bool>();
        List<StreamingChatCompletionUpdate> expectedCompletion =
        [
            new() { Role = ChatRole.Assistant, Text = "Chunk 1" },
        ];
        using var testClient = new TestChatClient
        {
            CompleteStreamingAsyncCallback = delegate
            {
                innerCallCount++;
                return ToAsyncEnumerableAsync(completionTcs.Task, expectedCompletion);
            }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        // Act 1: Concurrent calls before resolution are passed into the inner client
        var result1 = outer.CompleteStreamingAsync([new ChatMessage(ChatRole.User, "some input")]);
        var result2 = outer.CompleteStreamingAsync([new ChatMessage(ChatRole.User, "some input")]);

        // Assert 1
        Assert.NotSame(result1, result2);
        var result1Assertion = AssertCompletionsEqualAsync(expectedCompletion, result1);
        var result2Assertion = AssertCompletionsEqualAsync(expectedCompletion, result2);
        Assert.False(result1Assertion.IsCompleted);
        Assert.False(result2Assertion.IsCompleted);
        completionTcs.SetResult(true);
        await result1Assertion;
        await result2Assertion;
        Assert.Equal(2, innerCallCount);

        // Act 2: Subsequent calls after completion are resolved from the cache
        var result3 = outer.CompleteStreamingAsync([new ChatMessage(ChatRole.User, "some input")]);
        await AssertCompletionsEqualAsync(expectedCompletion, result3);
        Assert.Equal(2, innerCallCount);
    }

    [Fact]
    public async Task StreamingDoesNotCacheExceptionResultsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        using var testClient = new TestChatClient
        {
            CompleteStreamingAsyncCallback = delegate
            {
                innerCallCount++;
                return ToAsyncEnumerableAsync<StreamingChatCompletionUpdate>(Task.CompletedTask,
                [
                    () => new() { Role = ChatRole.Assistant, Text = "Chunk 1" },
                    () => throw new InvalidTimeZoneException("some failure"),
                ]);
            }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        var input = new ChatMessage(ChatRole.User, "abc");
        var result1 = outer.CompleteStreamingAsync([input]);
        var ex1 = await Assert.ThrowsAsync<InvalidTimeZoneException>(() => ToListAsync(result1));
        Assert.Equal("some failure", ex1.Message);
        Assert.Equal(1, innerCallCount);

        // Act
        var result2 = outer.CompleteStreamingAsync([input]);
        var ex2 = await Assert.ThrowsAsync<InvalidTimeZoneException>(() => ToListAsync(result2));

        // Assert
        Assert.NotSame(ex1, ex2);
        Assert.Equal("some failure", ex2.Message);
        Assert.Equal(2, innerCallCount);
    }

    [Fact]
    public async Task StreamingDoesNotCacheCanceledResultsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        var completionTcs = new TaskCompletionSource<bool>();
        using var testClient = new TestChatClient
        {
            CompleteStreamingAsyncCallback = delegate
            {
                innerCallCount++;
                return ToAsyncEnumerableAsync<StreamingChatCompletionUpdate>(
                    innerCallCount == 1 ? completionTcs.Task : Task.CompletedTask,
                    [() => new() { Role = ChatRole.Assistant, Text = "A good result" }]);
            }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        // First call gets cancelled
        var input = new ChatMessage(ChatRole.User, "abc");
        var result1 = outer.CompleteStreamingAsync([input]);
        var result1Assertion = ToListAsync(result1);
        Assert.False(result1Assertion.IsCompleted);
        completionTcs.SetCanceled();
        await Assert.ThrowsAsync<TaskCanceledException>(() => result1Assertion);
        Assert.True(result1Assertion.IsCanceled);
        Assert.Equal(1, innerCallCount);

        // Act/Assert: Second call can succeed
        var result2 = await ToListAsync(outer.CompleteStreamingAsync([input]));
        Assert.Equal("A good result", result2[0].Text);
        Assert.Equal(2, innerCallCount);
    }

    [Fact]
    public async Task CacheKeyVariesByChatOptionsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        var completionTcs = new TaskCompletionSource<bool>();
        using var testClient = new TestChatClient
        {
            CompleteAsyncCallback = async (_, options, _) =>
            {
                innerCallCount++;
                await Task.Yield();
                return new([new(ChatRole.Assistant, options!.AdditionalProperties!["someKey"]!.ToString())]);
            }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        // Act: Call with two different ChatOptions that have the same values
        var result1 = await outer.CompleteAsync([], new ChatOptions
        {
            AdditionalProperties = new() { { "someKey", "value 1" } }
        });
        var result2 = await outer.CompleteAsync([], new ChatOptions
        {
            AdditionalProperties = new() { { "someKey", "value 1" } }
        });

        // Assert: Same result
        Assert.Equal(1, innerCallCount);
        Assert.Equal("value 1", result1.Message.Text);
        Assert.Equal("value 1", result2.Message.Text);

        // Act: Call with two different ChatOptions that have different values
        var result3 = await outer.CompleteAsync([], new ChatOptions
        {
            AdditionalProperties = new() { { "someKey", "value 1" } }
        });
        var result4 = await outer.CompleteAsync([], new ChatOptions
        {
            AdditionalProperties = new() { { "someKey", "value 2" } }
        });

        // Assert: Different results
        Assert.Equal(2, innerCallCount);
        Assert.Equal("value 1", result3.Message.Text);
        Assert.Equal("value 2", result4.Message.Text);
    }

    [Fact]
    public async Task SubclassCanOverrideCacheKeyToVaryByChatOptionsAsync()
    {
        // Arrange
        var innerCallCount = 0;
        var completionTcs = new TaskCompletionSource<bool>();
        using var testClient = new TestChatClient
        {
            CompleteAsyncCallback = async (_, options, _) =>
            {
                innerCallCount++;
                await Task.Yield();
                return new([new(ChatRole.Assistant, options!.AdditionalProperties!["someKey"]!.ToString())]);
            }
        };
        using var outer = new CachingChatClientWithCustomKey(testClient, _storage)
        {
            JsonSerializerOptions = TestJsonSerializerContext.Default.Options
        };

        // Act: Call with two different ChatOptions
        var result1 = await outer.CompleteAsync([], new ChatOptions
        {
            AdditionalProperties = new() { { "someKey", "value 1" } }
        });
        var result2 = await outer.CompleteAsync([], new ChatOptions
        {
            AdditionalProperties = new() { { "someKey", "value 2" } }
        });

        // Assert: Different results
        Assert.Equal(2, innerCallCount);
        Assert.Equal("value 1", result1.Message.Text);
        Assert.Equal("value 2", result2.Message.Text);
    }

    [Fact]
    public async Task CanCacheCustomContentTypesAsync()
    {
        // Arrange
        var expectedCompletion = new ChatCompletion([
            new(new ChatRole("fakeRole"),
            [
                new CustomAIContent1("Hello", DateTime.Now),
                new CustomAIContent2("Goodbye", 42),
            ])
        ]);

        var serializerOptions = new JsonSerializerOptions(TestJsonSerializerContext.Default.Options);
        serializerOptions.TypeInfoResolver = serializerOptions.TypeInfoResolver!.WithAddedModifier(typeInfo =>
        {
            if (typeInfo.Type == typeof(AIContent))
            {
                foreach (var t in new Type[] { typeof(CustomAIContent1), typeof(CustomAIContent2) })
                {
                    typeInfo.PolymorphismOptions!.DerivedTypes.Add(new JsonDerivedType(t, t.Name));
                }
            }
        });
        serializerOptions.TypeInfoResolverChain.Add(CustomAIContentJsonContext.Default);

        var innerCallCount = 0;
        using var testClient = new TestChatClient
        {
            CompleteAsyncCallback = delegate
            {
                innerCallCount++;
                return Task.FromResult(expectedCompletion);
            }
        };
        using var outer = new DistributedCachingChatClient(testClient, _storage)
        {
            JsonSerializerOptions = serializerOptions
        };

        // Make the initial request and do a quick sanity check
        var result1 = await outer.CompleteAsync([new ChatMessage(ChatRole.User, "some input")]);
        AssertCompletionsEqual(expectedCompletion, result1);

        // Act
        var result2 = await outer.CompleteAsync([new ChatMessage(ChatRole.User, "some input")]);

        // Assert
        Assert.Equal(1, innerCallCount);
        AssertCompletionsEqual(expectedCompletion, result2);
        Assert.NotSame(result2.Message.Contents[0], expectedCompletion.Message.Contents[0]);
        Assert.NotSame(result2.Message.Contents[1], expectedCompletion.Message.Contents[1]);
    }

    [Fact]
    public async Task CanResolveIDistributedCacheFromDI()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton<IDistributedCache>(_storage)
            .BuildServiceProvider();
        using var testClient = new TestChatClient
        {
            CompleteAsyncCallback = delegate
            {
                return Task.FromResult(new ChatCompletion([
                    new(ChatRole.Assistant, [new TextContent("Hey")])]));
            }
        };
        using var outer = testClient
            .AsBuilder()
            .UseDistributedCache(configure: options =>
            {
                options.JsonSerializerOptions = TestJsonSerializerContext.Default.Options;
            })
            .Build(services);

        // Act: Make a request that should populate the cache
        Assert.Empty(_storage.Keys);
        var result = await outer.CompleteAsync([new ChatMessage(ChatRole.User, "some input")]);

        // Assert
        Assert.NotNull(result);
        Assert.Single(_storage.Keys);
    }

    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> values)
    {
        var result = new List<T>();
        await foreach (var v in values)
        {
            result.Add(v);
        }

        return result;
    }

    private static IAsyncEnumerable<T> ToAsyncEnumerableAsync<T>(IEnumerable<T> values)
        => ToAsyncEnumerableAsync(Task.CompletedTask, values);

    private static IAsyncEnumerable<T> ToAsyncEnumerableAsync<T>(Task preTask, IEnumerable<T> valueFactories)
        => ToAsyncEnumerableAsync(preTask, valueFactories.Select<T, Func<T>>(v => () => v));

    private static async IAsyncEnumerable<T> ToAsyncEnumerableAsync<T>(Task preTask, IEnumerable<Func<T>> values)
    {
        await preTask;

        foreach (var value in values)
        {
            await Task.Yield();
            yield return value();
        }
    }

    private static void AssertCompletionsEqual(ChatCompletion expected, ChatCompletion actual)
    {
        Assert.Equal(expected.CompletionId, actual.CompletionId);
        Assert.Equal(expected.Usage?.InputTokenCount, actual.Usage?.InputTokenCount);
        Assert.Equal(expected.Usage?.OutputTokenCount, actual.Usage?.OutputTokenCount);
        Assert.Equal(expected.Usage?.TotalTokenCount, actual.Usage?.TotalTokenCount);
        AssertUsageValuesEqual(expected.Usage?.AdditionalValues, actual.Usage?.AdditionalValues);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.ModelId, actual.ModelId);
        Assert.Equal(
            JsonSerializer.Serialize(expected.AdditionalProperties, TestJsonSerializerContext.Default.Options),
            JsonSerializer.Serialize(actual.AdditionalProperties, TestJsonSerializerContext.Default.Options));
        Assert.Equal(expected.Choices.Count, actual.Choices.Count);

        for (var i = 0; i < expected.Choices.Count; i++)
        {
            Assert.IsType(expected.Choices[i].GetType(), actual.Choices[i]);
            Assert.Equal(expected.Choices[i].Role, actual.Choices[i].Role);
            Assert.Equal(expected.Choices[i].Text, actual.Choices[i].Text);
            Assert.Equal(expected.Choices[i].Contents.Count, actual.Choices[i].Contents.Count);

            for (var itemIndex = 0; itemIndex < expected.Choices[i].Contents.Count; itemIndex++)
            {
                var expectedItem = expected.Choices[i].Contents[itemIndex];
                var actualItem = actual.Choices[i].Contents[itemIndex];
                Assert.IsType(expectedItem.GetType(), actualItem);

                if (expectedItem is FunctionCallContent expectedFcc)
                {
                    var actualFcc = (FunctionCallContent)actualItem;
                    Assert.Equal(expectedFcc.Name, actualFcc.Name);
                    Assert.Equal(expectedFcc.CallId, actualFcc.CallId);

                    // The correct JSON-round-tripping of AIContent/AIContent is not
                    // the responsibility of CachingChatClient, so not testing that here.
                    Assert.Equal(
                        JsonSerializer.Serialize(expectedFcc.Arguments, TestJsonSerializerContext.Default.Options),
                        JsonSerializer.Serialize(actualFcc.Arguments, TestJsonSerializerContext.Default.Options));
                }
            }
        }
    }

    private static void AssertUsageValuesEqual(AdditionalUsageValues? a, AdditionalUsageValues? b)
    {
        Assert.Equal(a is null, b is null);
        if (a is not null)
        {
            Assert.Equal(a.Count, b!.Count);
            foreach (var key in a.Keys)
            {
                Assert.Equal(a[key], b[key]);
            }
        }
    }

    private static async Task AssertCompletionsEqualAsync(IReadOnlyList<StreamingChatCompletionUpdate> expected, IAsyncEnumerable<StreamingChatCompletionUpdate> actual)
    {
        var actualEnumerator = actual.GetAsyncEnumerator();

        foreach (var expectedItem in expected)
        {
            Assert.True(await actualEnumerator.MoveNextAsync());

            var actualItem = actualEnumerator.Current;
            Assert.Equal(expectedItem.Text, actualItem.Text);
            Assert.Equal(expectedItem.ChoiceIndex, actualItem.ChoiceIndex);
            Assert.Equal(expectedItem.Role, actualItem.Role);
            Assert.Equal(expectedItem.Contents.Count, actualItem.Contents.Count);

            for (var itemIndex = 0; itemIndex < expectedItem.Contents.Count; itemIndex++)
            {
                var expectedItemItem = expectedItem.Contents[itemIndex];
                var actualItemItem = actualItem.Contents[itemIndex];
                Assert.IsType(expectedItemItem.GetType(), actualItemItem);

                if (expectedItemItem is FunctionCallContent expectedFcc)
                {
                    var actualFcc = (FunctionCallContent)actualItemItem;
                    Assert.Equal(expectedFcc.Name, actualFcc.Name);
                    Assert.Equal(expectedFcc.CallId, actualFcc.CallId);

                    // The correct JSON-round-tripping of AIContent/AIContent is not
                    // the responsibility of CachingChatClient, so not testing that here.
                    Assert.Equal(
                        JsonSerializer.Serialize(expectedFcc.Arguments, TestJsonSerializerContext.Default.Options),
                        JsonSerializer.Serialize(actualFcc.Arguments, TestJsonSerializerContext.Default.Options));
                }
                else if (expectedItemItem is UsageContent expectedUsage)
                {
                    var actualUsage = (UsageContent)actualItemItem;
                    Assert.Equal(expectedUsage.Details.InputTokenCount, actualUsage.Details.InputTokenCount);
                    Assert.Equal(expectedUsage.Details.OutputTokenCount, actualUsage.Details.OutputTokenCount);
                    Assert.Equal(expectedUsage.Details.TotalTokenCount, actualUsage.Details.TotalTokenCount);
                }
            }
        }

        Assert.False(await actualEnumerator.MoveNextAsync());
    }

    private sealed class CachingChatClientWithCustomKey(IChatClient innerClient, IDistributedCache storage)
        : DistributedCachingChatClient(innerClient, storage)
    {
        protected override string GetCacheKey(params ReadOnlySpan<object?> values)
        {
            var baseKey = base.GetCacheKey(values);
            foreach (var value in values)
            {
                if (value is ChatOptions options)
                {
                    return baseKey + options.AdditionalProperties?["someKey"]?.ToString();
                }
            }

            return baseKey;
        }
    }

    public class CustomAIContent1(string text, DateTime date) : AIContent
    {
        public string Text => text;
        public DateTime Date => date;
    }

    public class CustomAIContent2(string text, int number) : AIContent
    {
        public string Text => text;
        public int Number => number;
    }
}
