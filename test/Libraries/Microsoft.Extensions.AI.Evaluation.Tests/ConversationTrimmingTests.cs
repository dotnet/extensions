// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Tests;

[Experimental("EVAL001")]
public class ConversationTrimmingTests
{
    [Fact]
    public void TokenizerIsNull()
    {
        Tokenizer? tokenizer = null;
        IEnumerable<ChatMessage> conversation = ["Test message".ToUserMessage()];

        Assert.Throws<ArgumentNullException>(() => conversation.Trim(tokenizer!, tokenBudget: 10));
    }

    [Fact]
    public void ConversationIsNull()
    {
        Tokenizer tokenizer = new AsciiTokenizer();
        IEnumerable<ChatMessage>? conversation = null;

        Assert.Throws<ArgumentNullException>(() => conversation!.Trim(tokenizer, tokenBudget: 10));
    }

    [Fact]
    public void ConversationIsEmpty()
    {
        Tokenizer tokenizer = new AsciiTokenizer();
        IEnumerable<ChatMessage> conversation = Enumerable.Empty<ChatMessage>();
        const int TokenBudget = 100;

        (IEnumerable<ChatMessage> trimmedConversation, int remainingTokenBudget) =
            conversation.Trim(tokenizer, TokenBudget);

        Assert.Empty(trimmedConversation);
        Assert.Equal(TokenBudget, remainingTokenBudget);
    }

    [Fact]
    public void TokenBudgetIsNegative()
    {
        Tokenizer tokenizer = new AsciiTokenizer();
        var message1 = "Message 1".ToUserMessage();
        IEnumerable<ChatMessage> conversation = [message1];

        Assert.Throws<ArgumentOutOfRangeException>(() => conversation.Trim(tokenizer, tokenBudget: -1));
    }

    [Fact]
    public void TokenBudgetIsZero()
    {
        Tokenizer tokenizer = new AsciiTokenizer();
        var message1 = "Message 1".ToUserMessage();
        IEnumerable<ChatMessage> conversation = [message1];

        const int TokenBudget = 0;

        (IEnumerable<ChatMessage> trimmedConversation, int remainingTokenBudget) =
            conversation.Trim(tokenizer, TokenBudget);

        Assert.Empty(trimmedConversation);
        Assert.Equal(0, remainingTokenBudget);
    }

    [Fact]
    public void ConversationFitsWithinTokenBudget()
    {
        Tokenizer tokenizer = new AsciiTokenizer();
        var message1 = "Message 1".ToUserMessage();
        var message2 = "Message 22".ToAssistantMessage();
        var message3 = "Message 333".ToUserMessage();
        IEnumerable<ChatMessage> conversation = [message1, message2, message3];

        const int TokenBudget = 50;
        int retainedTokens = message3.Text.Length + message2.Text.Length + message1.Text.Length;

        (IEnumerable<ChatMessage> trimmedConversation, int remainingTokenBudget) =
            conversation.Trim(tokenizer, TokenBudget);

        List<ChatMessage> trimmedConversationList = trimmedConversation.ToList();
        Assert.Equal(3, trimmedConversationList.Count);
        Assert.Equal(message3, trimmedConversationList[0]);
        Assert.Equal(message2, trimmedConversationList[1]);
        Assert.Equal(message1, trimmedConversationList[2]);
        Assert.Equal(TokenBudget - retainedTokens, remainingTokenBudget);
    }

    [Fact]
    public void ConversationFitsExactlyInTokenBudget()
    {
        Tokenizer tokenizer = new AsciiTokenizer();
        var message1 = "Message 1".ToUserMessage();
        var message2 = "Message 22".ToAssistantMessage();
        var message3 = "Message 333".ToUserMessage();
        IEnumerable<ChatMessage> conversation = [message1, message2, message3];

        const int TokenBudget = 30;

        (IEnumerable<ChatMessage> trimmedConversation, int remainingTokenBudget) =
            conversation.Trim(tokenizer, TokenBudget);

        List<ChatMessage> trimmedConversationList = trimmedConversation.ToList();
        Assert.Equal(3, trimmedConversationList.Count);
        Assert.Equal(message3, trimmedConversationList[0]);
        Assert.Equal(message2, trimmedConversationList[1]);
        Assert.Equal(message1, trimmedConversationList[2]);
        Assert.Equal(0, remainingTokenBudget);
    }

    [Fact]
    public void ConversationExceedsTokenBudget()
    {
        Tokenizer tokenizer = new AsciiTokenizer();
        var message1 = "Message 1".ToUserMessage();
        var message2 = "Message 22".ToAssistantMessage();
        var message3 = "Message 333".ToUserMessage();
        IEnumerable<ChatMessage> conversation = [message1, message2, message3];

        const int TokenBudget = 25;
        int retainedTokens = message3.Text.Length + message2.Text.Length;

        (IEnumerable<ChatMessage> trimmedConversation, int remainingTokenBudget) =
            conversation.Trim(tokenizer, TokenBudget);

        List<ChatMessage> trimmedConversationList = trimmedConversation.ToList();
        Assert.Equal(2, trimmedConversationList.Count);
        Assert.Equal(message3, trimmedConversationList[0]);
        Assert.Equal(message2, trimmedConversationList[1]);
        Assert.Equal(TokenBudget - retainedTokens, remainingTokenBudget);
    }

    [Fact]
    public void EveryMessageInConversationExceedsTokenBudget()
    {
        Tokenizer tokenizer = new AsciiTokenizer();
        var message1 = "Message 1".ToUserMessage();
        var message2 = "Message 22".ToAssistantMessage();
        IEnumerable<ChatMessage> conversation = [message1, message2];

        const int TokenBudget = 5;

        (IEnumerable<ChatMessage> trimmedConversation, int remainingTokenBudget) =
            conversation.Trim(tokenizer, TokenBudget);

        Assert.Empty(trimmedConversation);
        Assert.Equal(TokenBudget, remainingTokenBudget);
    }

    [Fact]
    public void LastMessageInConversationFitsTokenBudgetExactly()
    {
        Tokenizer tokenizer = new AsciiTokenizer();
        var message1 = "Message 1".ToUserMessage();
        var message2 = "Message 22".ToAssistantMessage();
        var message3 = "Message 333".ToUserMessage();
        IEnumerable<ChatMessage> conversation = [message1, message2, message3];

        const int TokenBudget = 11;

        (IEnumerable<ChatMessage> trimmedConversation, int remainingTokenBudget) =
            conversation.Trim(tokenizer, TokenBudget);

        List<ChatMessage> trimmedConversationList = trimmedConversation.ToList();
        Assert.Single(trimmedConversationList);
        Assert.Equal(message3, trimmedConversationList[0]);
        Assert.Equal(0, remainingTokenBudget);
    }

    [Fact]
    public void SubsetOfMessagesInConversationFitTokenBudgetExactly()
    {
        Tokenizer tokenizer = new AsciiTokenizer();
        var message1 = "Message 1".ToUserMessage();
        var message2 = "Message 22".ToAssistantMessage();
        var message3 = "Message 333".ToUserMessage();
        IEnumerable<ChatMessage> conversation = [message1, message2, message3];

        const int TokenBudget = 21;

        (IEnumerable<ChatMessage> trimmedConversation, int remainingTokenBudget) =
            conversation.Trim(tokenizer, TokenBudget);

        List<ChatMessage> trimmedConversationList = trimmedConversation.ToList();
        Assert.Equal(2, trimmedConversationList.Count);
        Assert.Equal(message3, trimmedConversationList[0]);
        Assert.Equal(message2, trimmedConversationList[1]);
        Assert.Equal(0, remainingTokenBudget);
    }
}
