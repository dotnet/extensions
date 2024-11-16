// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ConfigureOptionsChatClientTests
{
    [Fact]
    public void ConfigureOptionsChatClient_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new ConfigureOptionsChatClient(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => new ConfigureOptionsChatClient(new TestChatClient(), null!));
    }

    [Fact]
    public void ConfigureOptions_InvalidArgs_Throws()
    {
        using var innerClient = new TestChatClient();
        var builder = innerClient.ToBuilder();
        Assert.Throws<ArgumentNullException>("configure", () => builder.ConfigureOptions(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigureOptions_ReturnedInstancePassedToNextClient(bool nullProvidedOptions)
    {
        ChatOptions? providedOptions = nullProvidedOptions ? null : new() { ModelId = "test" };
        ChatOptions? returnedOptions = null;
        ChatCompletion expectedCompletion = new(Array.Empty<ChatMessage>());
        var expectedUpdates = Enumerable.Range(0, 3).Select(i => new StreamingChatCompletionUpdate()).ToArray();
        using CancellationTokenSource cts = new();

        using IChatClient innerClient = new TestChatClient
        {
            CompleteAsyncCallback = (messages, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedCompletion);
            },

            CompleteStreamingAsyncCallback = (messages, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return YieldUpdates(expectedUpdates);
            },
        };

        using var client = innerClient
            .ToBuilder()
            .ConfigureOptions(options =>
            {
                Assert.NotSame(providedOptions, options);
                if (nullProvidedOptions)
                {
                    Assert.Null(options.ModelId);
                }
                else
                {
                    Assert.Equal(providedOptions!.ModelId, options.ModelId);
                }

                returnedOptions = options;
            })
            .Build();

        var completion = await client.CompleteAsync(Array.Empty<ChatMessage>(), providedOptions, cts.Token);
        Assert.Same(expectedCompletion, completion);

        int i = 0;
        await using var e = client.CompleteStreamingAsync(Array.Empty<ChatMessage>(), providedOptions, cts.Token).GetAsyncEnumerator();
        while (i < expectedUpdates.Length)
        {
            Assert.True(await e.MoveNextAsync());
            Assert.Same(expectedUpdates[i++], e.Current);
        }

        Assert.False(await e.MoveNextAsync());

        static async IAsyncEnumerable<StreamingChatCompletionUpdate> YieldUpdates(StreamingChatCompletionUpdate[] updates)
        {
            foreach (var update in updates)
            {
                await Task.Yield();
                yield return update;
            }
        }
    }
}
