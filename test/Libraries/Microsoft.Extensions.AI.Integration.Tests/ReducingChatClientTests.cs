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
                Assert.Equal(2, messages.Count);
                Assert.Collection(messages,
                    m => Assert.StartsWith("Golden retrievers are quite active", m.Text, StringComparison.Ordinal),
                    m => Assert.StartsWith("Are they good with kids?", m.Text, StringComparison.Ordinal));
                return Task.FromResult(new ChatResponse([]));
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
    private readonly bool _inPlace;

    /// <summary>Initializes a new instance of the <see cref="ReducingChatClient"/> class.</summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="reducer">The reducer to be used by this instance.</param>
    /// <param name="inPlace">
    /// true if the <paramref name="reducer"/> should perform any modifications directly on the supplied list of messages;
    /// false if it should instead create a new list when reduction is necessary.
    /// </param>
    public ReducingChatClient(IChatClient innerClient, IChatReducer reducer, bool inPlace = false)
        : base(innerClient)
    {
        _reducer = Throw.IfNull(reducer);
        _inPlace = inPlace;
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        chatMessages = await GetChatMessagesToPropagate(chatMessages, cancellationToken).ConfigureAwait(false);

        return await base.GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        chatMessages = await GetChatMessagesToPropagate(chatMessages, cancellationToken).ConfigureAwait(false);

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    /// <summary>Runs the reducer and gets the chat message list to forward to the inner client.</summary>
    private async Task<IList<ChatMessage>> GetChatMessagesToPropagate(IList<ChatMessage> chatMessages, CancellationToken cancellationToken) =>
        await _reducer.ReduceAsync(chatMessages, _inPlace, cancellationToken).ConfigureAwait(false) ??
        chatMessages;
}

/// <summary>Represents a reducer capable of shrinking the size of a list of chat messages.</summary>
public interface IChatReducer
{
    /// <summary>Reduces the size of a list of chat messages.</summary>
    /// <param name="chatMessages">The messages.</param>
    /// <param name="inPlace">true if the reducer should modify the provided list; false if a new list should be returned.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The new list of messages, or null if no reduction need be performed or <paramref name="inPlace"/> was true.</returns>
    Task<IList<ChatMessage>?> ReduceAsync(IList<ChatMessage> chatMessages, bool inPlace, CancellationToken cancellationToken);
}

/// <summary>Provides extensions for configuring <see cref="ReducingChatClientExtensions"/> instances.</summary>
public static class ReducingChatClientExtensions
{
    public static ChatClientBuilder UseChatReducer(this ChatClientBuilder builder, IChatReducer reducer, bool inPlace = false)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(reducer);

        return builder.Use(innerClient => new ReducingChatClient(innerClient, reducer, inPlace));
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

    public async Task<IList<ChatMessage>?> ReduceAsync(IList<ChatMessage> chatMessages, bool inPlace, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(chatMessages);

        if (chatMessages.Count > 1)
        {
            int totalCount = CountTokens(chatMessages[chatMessages.Count - 1]);

            if (inPlace)
            {
                for (int i = chatMessages.Count - 2; i >= 0; i--)
                {
                    totalCount += CountTokens(chatMessages[i]);
                    if (totalCount > _tokenLimit)
                    {
                        if (chatMessages is List<ChatMessage> list)
                        {
                            list.RemoveRange(0, i + 1);
                        }
                        else
                        {
                            for (int j = i; j >= 0; j--)
                            {
                                chatMessages.RemoveAt(j);
                            }
                        }

                        break;
                    }
                }
            }
            else
            {
                for (int i = chatMessages.Count - 2; i >= 0; i--)
                {
                    totalCount += CountTokens(chatMessages[i]);
                    if (totalCount > _tokenLimit)
                    {
                        return chatMessages.Skip(i + 1).ToList();
                    }
                }
            }
        }

        return null;
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
