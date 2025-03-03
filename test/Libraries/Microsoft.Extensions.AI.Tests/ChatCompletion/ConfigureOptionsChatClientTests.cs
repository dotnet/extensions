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
        var builder = innerClient.AsBuilder();
        Assert.Throws<ArgumentNullException>("configure", () => builder.ConfigureOptions(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigureOptions_ReturnedInstancePassedToNextClient(bool nullProvidedOptions)
    {
        ChatOptions? providedOptions = nullProvidedOptions ? null : new() { ModelId = "test" };
        ChatOptions? returnedOptions = null;
        ChatResponse expectedResponse = new(Array.Empty<ChatMessage>());
        var expectedUpdates = Enumerable.Range(0, 3).Select(i => new ChatResponseUpdate()).ToArray();
        using CancellationTokenSource cts = new();

        using IChatClient innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedResponse);
            },

            GetStreamingResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                Assert.Same(returnedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return YieldUpdates(expectedUpdates);
            },
        };

        using var client = innerClient
            .AsBuilder()
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

        var response = await client.GetResponseAsync(Array.Empty<ChatMessage>(), providedOptions, cts.Token);
        Assert.Same(expectedResponse, response);

        int i = 0;
        await using var e = client.GetStreamingResponseAsync(Array.Empty<ChatMessage>(), providedOptions, cts.Token).GetAsyncEnumerator();
        while (i < expectedUpdates.Length)
        {
            Assert.True(await e.MoveNextAsync());
            Assert.Same(expectedUpdates[i++], e.Current);
        }

        Assert.False(await e.MoveNextAsync());

        static async IAsyncEnumerable<ChatResponseUpdate> YieldUpdates(ChatResponseUpdate[] updates)
        {
            foreach (var update in updates)
            {
                await Task.Yield();
                yield return update;
            }
        }
    }
}
