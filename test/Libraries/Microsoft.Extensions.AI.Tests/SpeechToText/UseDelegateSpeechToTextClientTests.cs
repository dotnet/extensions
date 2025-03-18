// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class UseDelegateSpeechToTextClientTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        using var client = new TestSpeechToTextClient();
        SpeechToTextClientBuilder builder = new(client);

        Assert.Throws<ArgumentNullException>("sharedFunc", () =>
            builder.Use((AnonymousDelegatingSpeechToTextClient.GetResponseSharedFunc)null!));

        Assert.Throws<ArgumentNullException>("getResponseFunc", () => builder.Use(null!, null!));

        Assert.Throws<ArgumentNullException>("innerClient", () => new AnonymousDelegatingSpeechToTextClient(null!, delegate { return Task.CompletedTask; }));
        Assert.Throws<ArgumentNullException>("sharedFunc", () => new AnonymousDelegatingSpeechToTextClient(client, null!));

        Assert.Throws<ArgumentNullException>("innerClient", () => new AnonymousDelegatingSpeechToTextClient(null!, null!, null!));
        Assert.Throws<ArgumentNullException>("getResponseFunc", () => new AnonymousDelegatingSpeechToTextClient(client, null!, null!));
    }

    [Fact]
    public async Task Shared_ContextPropagated()
    {
        IList<IAsyncEnumerable<DataContent>> expectedContents = [];
        SpeechToTextOptions expectedOptions = new();
        using CancellationTokenSource expectedCts = new();

        AsyncLocal<int> asyncLocal = new();

        using ISpeechToTextClient innerClient = new TestSpeechToTextClient
        {
            GetResponseAsyncCallback = (speechContentsList, options, cancellationToken) =>
            {
                Assert.Same(expectedContents, speechContentsList);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return Task.FromResult(new SpeechToTextResponse(new SpeechToTextMessage { Text = "hello" }));
            },

            GetStreamingResponseAsyncCallback = (speechContentsList, options, cancellationToken) =>
            {
                Assert.Same(expectedContents, speechContentsList);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return YieldUpdates(new SpeechToTextResponseUpdate { Text = "world" });
            },
        };

        using ISpeechToTextClient client = new SpeechToTextClientBuilder(innerClient)
            .Use(async (chatMessages, options, next, cancellationToken) =>
            {
                Assert.Same(expectedContents, chatMessages);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                asyncLocal.Value = 42;
                await next(chatMessages, options, cancellationToken);
            })
            .Build();

        Assert.Equal(0, asyncLocal.Value);
        SpeechToTextResponse response = await client.GetResponseAsync(expectedContents, expectedOptions, expectedCts.Token);
        Assert.Equal("hello", response.Message.Text);

        Assert.Equal(0, asyncLocal.Value);
        response = await client.GetStreamingResponseAsync(expectedContents, expectedOptions, expectedCts.Token).ToSpeechToTextResponseAsync();
        Assert.Equal("world", response.Message.Text);
    }

    [Fact]
    public async Task GetResponseFunc_ContextPropagated()
    {
        IList<IAsyncEnumerable<DataContent>> expectedContents = [];
        SpeechToTextOptions expectedOptions = new();
        using CancellationTokenSource expectedCts = new();
        AsyncLocal<int> asyncLocal = new();

        using ISpeechToTextClient innerClient = new TestSpeechToTextClient
        {
            GetResponseAsyncCallback = (speechContentsList, options, cancellationToken) =>
            {
                Assert.Same(expectedContents, speechContentsList);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return Task.FromResult(new SpeechToTextResponse(new SpeechToTextMessage { Text = "hello" }));
            },
        };

        using ISpeechToTextClient client = new SpeechToTextClientBuilder(innerClient)
            .Use(async (speechContentsList, options, innerClient, cancellationToken) =>
            {
                Assert.Same(expectedContents, speechContentsList);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                asyncLocal.Value = 42;
                var cc = await innerClient.GetResponseAsync(speechContentsList, options, cancellationToken);
                cc.Choices[0].Text += " world";
                return cc;
            }, null)
            .Build();

        Assert.Equal(0, asyncLocal.Value);

        SpeechToTextResponse response = await client.GetResponseAsync(expectedContents, expectedOptions, expectedCts.Token);
        Assert.Equal("hello world", response.Message.Text);

        response = await client.GetStreamingResponseAsync(expectedContents, expectedOptions, expectedCts.Token).ToSpeechToTextResponseAsync();
        Assert.Equal("hello world", response.Message.Text);
    }

    [Fact]
    public async Task GetStreamingResponseFunc_ContextPropagated()
    {
        IList<IAsyncEnumerable<DataContent>> expectedContents = [];
        SpeechToTextOptions expectedOptions = new();
        using CancellationTokenSource expectedCts = new();
        AsyncLocal<int> asyncLocal = new();

        using ISpeechToTextClient innerClient = new TestSpeechToTextClient
        {
            GetStreamingResponseAsyncCallback = (speechContentsList, options, cancellationToken) =>
            {
                Assert.Same(expectedContents, speechContentsList);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return YieldUpdates(new SpeechToTextResponseUpdate { Text = "hello" });
            },
        };

        using ISpeechToTextClient client = new SpeechToTextClientBuilder(innerClient)
            .Use(null, (speechContentsList, options, innerClient, cancellationToken) =>
            {
                Assert.Same(expectedContents, speechContentsList);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                asyncLocal.Value = 42;
                return Impl(speechContentsList, options, innerClient, cancellationToken);

                static async IAsyncEnumerable<SpeechToTextResponseUpdate> Impl(
                    IList<IAsyncEnumerable<DataContent>> speechContentsList,
                    SpeechToTextOptions? options,
                    ISpeechToTextClient innerClient,
                    [EnumeratorCancellation] CancellationToken cancellationToken)
                {
                    await foreach (var update in innerClient.GetStreamingResponseAsync(speechContentsList, options, cancellationToken))
                    {
                        yield return update;
                    }

                    yield return new() { Text = " world" };
                }
            })
            .Build();

        Assert.Equal(0, asyncLocal.Value);

        SpeechToTextResponse response = await client.GetResponseAsync(expectedContents, expectedOptions, expectedCts.Token);
        Assert.Equal("hello world", response.Message.Text);

        response = await client.GetStreamingResponseAsync(expectedContents, expectedOptions, expectedCts.Token).ToSpeechToTextResponseAsync();
        Assert.Equal("hello world", response.Message.Text);
    }

    [Fact]
    public async Task BothGetResponseAndGetStreamingResponseFuncs_ContextPropagated()
    {
        IList<IAsyncEnumerable<DataContent>> expectedContents = [];
        SpeechToTextOptions expectedOptions = new();
        using CancellationTokenSource expectedCts = new();
        AsyncLocal<int> asyncLocal = new();

        using ISpeechToTextClient innerClient = new TestSpeechToTextClient
        {
            GetResponseAsyncCallback = (speechContentsList, options, cancellationToken) =>
            {
                Assert.Same(expectedContents, speechContentsList);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return Task.FromResult(new SpeechToTextResponse(new SpeechToTextMessage { Text = "non-streaming hello" }));
            },

            GetStreamingResponseAsyncCallback = (speechContentsList, options, cancellationToken) =>
            {
                Assert.Same(expectedContents, speechContentsList);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCts.Token, cancellationToken);
                Assert.Equal(42, asyncLocal.Value);
                return YieldUpdates(new SpeechToTextResponseUpdate { Text = "streaming hello" });
            },
        };

        using ISpeechToTextClient client = new SpeechToTextClientBuilder(innerClient)
            .Use(
                async (speechContentsList, options, innerClient, cancellationToken) =>
                {
                    Assert.Same(expectedContents, speechContentsList);
                    Assert.Same(expectedOptions, options);
                    Assert.Equal(expectedCts.Token, cancellationToken);
                    asyncLocal.Value = 42;
                    var cc = await innerClient.GetResponseAsync(speechContentsList, options, cancellationToken);
                    cc.Choices[0].Text += " world (non-streaming)";
                    return cc;
                },
                (speechContentsList, options, innerClient, cancellationToken) =>
                {
                    Assert.Same(expectedContents, speechContentsList);
                    Assert.Same(expectedOptions, options);
                    Assert.Equal(expectedCts.Token, cancellationToken);
                    asyncLocal.Value = 42;
                    return Impl(speechContentsList, options, innerClient, cancellationToken);

                    static async IAsyncEnumerable<SpeechToTextResponseUpdate> Impl(
                        IList<IAsyncEnumerable<DataContent>> speechContentsList,
                        SpeechToTextOptions? options,
                        ISpeechToTextClient innerClient,
                        [EnumeratorCancellation] CancellationToken cancellationToken)
                    {
                        await foreach (var update in innerClient.GetStreamingResponseAsync(speechContentsList, options, cancellationToken))
                        {
                            yield return update;
                        }

                        yield return new() { Text = " world (streaming)" };
                    }
                })
            .Build();

        Assert.Equal(0, asyncLocal.Value);

        SpeechToTextResponse response = await client.GetResponseAsync(expectedContents, expectedOptions, expectedCts.Token);
        Assert.Equal("non-streaming hello world (non-streaming)", response.Message.Text);

        response = await client.GetStreamingResponseAsync(expectedContents, expectedOptions, expectedCts.Token).ToSpeechToTextResponseAsync();
        Assert.Equal("streaming hello world (streaming)", response.Message.Text);
    }

    private static async IAsyncEnumerable<SpeechToTextResponseUpdate> YieldUpdates(params SpeechToTextResponseUpdate[] updates)
    {
        foreach (var update in updates)
        {
            await Task.Yield();
            yield return update;
        }
    }
}
