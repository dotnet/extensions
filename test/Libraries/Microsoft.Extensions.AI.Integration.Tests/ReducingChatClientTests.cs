// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.Tokenizers;
using Microsoft.Shared.Diagnostics;
using Xunit;

#pragma warning disable S103 // Lines should not be too long
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Microsoft.Extensions.AI;

/// <summary>Provides an example of a custom <see cref="IChatClient"/> for reducing chat message lists.</summary>
public class ReducingChatClientTests
{
    private static readonly Tokenizer _gpt4oTokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");

    [Fact]
    public async Task Reduction_LimitsMessagesBasedOnTokenLimit()
    {
        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, cancellationToken) =>
            {
                Assert.Equal(2, messages.Count());
                Assert.Collection(messages,
                    m => Assert.StartsWith("Golden retrievers are quite active", m.Text, StringComparison.Ordinal),
                    m => Assert.StartsWith("Are they good with kids?", m.Text, StringComparison.Ordinal));
                return Task.FromResult(new ChatResponse());
            }
        };

        using var client = innerClient
            .AsBuilder()
            .UseChatReducer(new TokenCountingChatReducer(_gpt4oTokenizer, 40))
            .Build();

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.User, "Hi there! Can you tell me about golden retrievers?"),
            new ChatMessage(ChatRole.Assistant, "Of course! Golden retrievers are known for their friendly and tolerant attitudes. They're great family pets and are very intelligent and easy to train."),
            new ChatMessage(ChatRole.User, "What kind of exercise do they need?"),
            new ChatMessage(ChatRole.Assistant, "Golden retrievers are quite active and need regular exercise. Daily walks, playtime, and activities like fetching or swimming are great for them."),
            new ChatMessage(ChatRole.User, "Are they good with kids?"),
        ];

        await client.GetResponseAsync(messages);

        Assert.Equal(5, messages.Count);
    }
}

/// <summary>Provides an example of a chat client for reducing the size of a message list.</summary>
public sealed class ReducingChatClient : DelegatingChatClient
{
    private readonly IChatReducer _reducer;

    /// <summary>Initializes a new instance of the <see cref="ReducingChatClient"/> class.</summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="reducer">The reducer to be used by this instance.</param>
    public ReducingChatClient(IChatClient innerClient, IChatReducer reducer)
        : base(innerClient)
    {
        _reducer = Throw.IfNull(reducer);
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        messages = await _reducer.ReduceAsync(messages, cancellationToken).ConfigureAwait(false);

        return await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        messages = await _reducer.ReduceAsync(messages, cancellationToken).ConfigureAwait(false);

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }
}

/// <summary>Represents a reducer capable of shrinking the size of a list of chat messages.</summary>
public interface IChatReducer
{
    /// <summary>Reduces the size of a list of chat messages.</summary>
    /// <param name="messages">The messages.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The new list of messages, or <see langword="null"/> if no reduction need be performed or <paramref name="inPlace"/> was true.</returns>
    Task<IList<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken);
}

/// <summary>Provides extensions for configuring <see cref="ReducingChatClientExtensions"/> instances.</summary>
public static class ReducingChatClientExtensions
{
    public static ChatClientBuilder UseChatReducer(this ChatClientBuilder builder, IChatReducer reducer)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(reducer);

        return builder.Use(innerClient => new ReducingChatClient(innerClient, reducer));
    }
}

/// <summary>An <see cref="IChatReducer"/> that culls the oldest messages once a certain token threshold is reached.</summary>
public sealed class TokenCountingChatReducer : IChatReducer
{
    private readonly Tokenizer _tokenizer;
    private readonly int _tokenLimit;

    public TokenCountingChatReducer(Tokenizer tokenizer, int tokenLimit)
    {
        _tokenizer = Throw.IfNull(tokenizer);
        _tokenLimit = Throw.IfLessThan(tokenLimit, 1);
    }

    public async Task<IList<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(messages);

        List<ChatMessage> list = messages.ToList();

        if (list.Count > 1)
        {
            int totalCount = CountTokens(list[list.Count - 1]);

            for (int i = list.Count - 2; i >= 0; i--)
            {
                totalCount += CountTokens(list[i]);
                if (totalCount > _tokenLimit)
                {
                    list.RemoveRange(0, i + 1);
                    break;
                }
            }
        }

        return list;
    }

    private int CountTokens(ChatMessage message)
    {
        int sum = 0;
        foreach (AIContent content in message.Contents)
        {
            if (content is TextContent text)
            {
                sum += _tokenizer.CountTokens(text.Text);
            }
        }

        return sum;
    }
}
