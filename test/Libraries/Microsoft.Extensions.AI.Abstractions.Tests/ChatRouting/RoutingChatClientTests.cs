// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class RoutingChatClientTests
{
    [Fact]
    public void Create_RejectsNullSelector()
    {
        Assert.Throws<ArgumentNullException>(() => RoutingChatClient.Create(null!));
    }

    [Fact]
    public async Task Create_SelectsClientForRequest()
    {
        var messages = new ChatMessage[] { new(ChatRole.User, "hi") };
        var options = new ChatOptions { ModelId = "request" };
        using var cancellationSource = new CancellationTokenSource();
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "ok"));
        ChatOptions? forwardedOptions = null;
        using var selected = new TestChatClient
        {
            GetResponseAsyncCallback = (_, selectedOptions, _) =>
            {
                forwardedOptions = selectedOptions;
                return Task.FromResult(expected);
            },
        };
        RoutingContext? observedContext = null;
        CancellationToken observedToken = default;
        int selectionCount = 0;
        using RoutingChatClient router = RoutingChatClient.Create((context, cancellationToken) =>
        {
            observedContext = context;
            observedToken = cancellationToken;
            selectionCount++;
            context.ChatOptions!.ModelId = "selected";
            return new(selected);
        });

        ChatResponse response = await router.GetResponseAsync(messages, options, cancellationSource.Token);

        Assert.Same(expected, response);
        Assert.Same(messages, observedContext!.Messages);
        Assert.NotSame(options, observedContext.ChatOptions);
        Assert.NotSame(observedContext.ChatOptions, forwardedOptions);
        Assert.Equal("selected", observedContext.ChatOptions!.ModelId);
        Assert.Equal("request", forwardedOptions!.ModelId);
        Assert.Equal("request", options.ModelId);
        Assert.Equal(cancellationSource.Token, observedToken);
        Assert.Equal(1, selectionCount);
    }

    [Fact]
    public async Task Create_DoesNotReselectAfterFailure()
    {
        var expected = new InvalidOperationException("failed");
        using var selected = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => throw expected,
        };
        int selectionCount = 0;
        using RoutingChatClient router = RoutingChatClient.Create((_, _) =>
        {
            selectionCount++;
            return new(selected);
        });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Same(expected, actual);
        Assert.Equal(1, selectionCount);
    }

    [Fact]
    public async Task Create_NullSelectionThrowsForNonStreamingAndStreaming()
    {
        using RoutingChatClient router = RoutingChatClient.Create((_, _) => new((IChatClient)null!));

        InvalidOperationException nonStreaming = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));
        InvalidOperationException streaming = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CollectAsync(router.GetStreamingResponseAsync([new(ChatRole.User, "hi")])));

        Assert.Contains("SelectClientAsync", nonStreaming.Message, StringComparison.Ordinal);
        Assert.Contains("SelectClientAsync", streaming.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_RejectsNullMessagesForNonStreamingAndStreaming()
    {
        using var selected = new TestChatClient();
        using RoutingChatClient router = RoutingChatClient.Create((_, _) => new(selected));

        await Assert.ThrowsAsync<ArgumentNullException>(() => router.GetResponseAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => CollectAsync(router.GetStreamingResponseAsync(null!)));
    }

    [Fact]
    public async Task Create_DoesNotDisposeSelectedClient()
    {
        using var selected = new CountingDisposeClient();
        using (RoutingChatClient router = RoutingChatClient.Create((_, _) => new(selected)))
        {
            _ = await router.GetResponseAsync([new(ChatRole.User, "hi")]);
        }

        Assert.Equal(0, selected.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateContext_SuppliesCustomContextToSelection(bool streaming)
    {
        ChatResponse expected = new(new ChatMessage(ChatRole.Assistant, "ok"));
        using var selected = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) => Task.FromResult(expected),
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldUpdates("ok"),
        };
        using var router = new CustomContextTestRouter(selected);

        ChatResponse response = streaming
            ? await router.GetStreamingResponseAsync([new(ChatRole.User, "hi")]).ToChatResponseAsync()
            : await router.GetResponseAsync([new(ChatRole.User, "hi")]);

        Assert.Equal("ok", response.Text);
        Assert.Equal(1, router.ContextsCreated);
        Assert.Equal(1, router.CustomContextsObserved);
    }

    [Fact]
    public async Task CreateContext_NullResultThrows()
    {
        using var selected = new TestChatClient();
        using var router = new NullContextTestRouter(selected);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => router.GetResponseAsync([new(ChatRole.User, "hi")]));

        Assert.Contains("CreateContext", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetService_ReturnsSelfAndNullForUnknownOrKeyed()
    {
        using var client = new DelegatingTestRouter(_ => throw new NotSupportedException());

        Assert.Same(client, client.GetService(typeof(DelegatingTestRouter)));
        Assert.Same(client, client.GetService(typeof(RoutingChatClient)));
        Assert.Same(client, client.GetService(typeof(IChatClient)));
        Assert.Null(client.GetService(typeof(DelegatingTestRouter), serviceKey: "key"));
        Assert.Null(client.GetService(typeof(string)));
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldUpdates(params string[] texts)
    {
        foreach (string text in texts)
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, text);
        }
    }

    private static async Task<List<ChatResponseUpdate>> CollectAsync(
        IAsyncEnumerable<ChatResponseUpdate> updates)
    {
        var result = new List<ChatResponseUpdate>();
        await foreach (ChatResponseUpdate update in updates)
        {
            result.Add(update);
        }

        return result;
    }

    private sealed class DelegatingTestRouter : RoutingChatClient
    {
        private readonly Func<RoutingContext, IChatClient> _select;

        public DelegatingTestRouter(Func<RoutingContext, IChatClient> select)
        {
            _select = select;
        }

        protected override ValueTask<IChatClient> SelectClientAsync(
            RoutingContext context,
            CancellationToken cancellationToken) =>
            new(_select(context));
    }

    private sealed class CustomContextTestRouter : RoutingChatClient
    {
        private readonly IChatClient _client;

        public CustomContextTestRouter(IChatClient client)
        {
            _client = client;
        }

        public int ContextsCreated { get; private set; }

        public int CustomContextsObserved { get; private set; }

        protected override RoutingContext CreateContext(
            IEnumerable<ChatMessage> messages, ChatOptions? options)
        {
            ContextsCreated++;
            return new TaggedRoutingContext(messages, options);
        }

        protected override ValueTask<IChatClient> SelectClientAsync(
            RoutingContext context,
            CancellationToken cancellationToken)
        {
            if (context is TaggedRoutingContext)
            {
                CustomContextsObserved++;
            }

            return new(_client);
        }

        private sealed class TaggedRoutingContext(IEnumerable<ChatMessage> messages, ChatOptions? chatOptions)
            : RoutingContext(messages, chatOptions);
    }

    private sealed class NullContextTestRouter : RoutingChatClient
    {
        private readonly IChatClient _client;

        public NullContextTestRouter(IChatClient client)
        {
            _client = client;
        }

        protected override RoutingContext CreateContext(
            IEnumerable<ChatMessage> messages, ChatOptions? options) =>
            null!;

        protected override ValueTask<IChatClient> SelectClientAsync(
            RoutingContext context,
            CancellationToken cancellationToken) =>
            new(_client);
    }

    private sealed class CountingDisposeClient : IChatClient
    {
        public int DisposeCount { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse());

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() => DisposeCount++;

        public override bool Equals(object? obj) => obj is CountingDisposeClient;

        public override int GetHashCode() => 0;
    }
}
