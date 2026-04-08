// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using OpenAI;
using OpenAI.Conversations;
using Xunit;

#pragma warning disable MEAI001
#pragma warning disable OPENAI001

namespace Microsoft.Extensions.AI;

public class OpenAIHostedConversationClientTests
{
    [Fact]
    public void AsIHostedConversationClient_NullClient_Throws()
    {
        Assert.Throws<ArgumentNullException>("conversationClient", () => ((ConversationClient)null!).AsIHostedConversationClient());
    }

    [Fact]
    public void GetService_ReturnsMetadata()
    {
        Uri endpoint = new("http://localhost/some/endpoint");
        ConversationClient conversationClient = new(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint });

        IHostedConversationClient client = conversationClient.AsIHostedConversationClient();

        var metadata = client.GetService(typeof(HostedConversationClientMetadata)) as HostedConversationClientMetadata;
        Assert.NotNull(metadata);
        Assert.Equal("openai", metadata.ProviderName);
        Assert.Equal(endpoint, metadata.ProviderUri);
    }

    [Fact]
    public void GetService_ReturnsConversationClient()
    {
        ConversationClient conversationClient = new(new ApiKeyCredential("key"));

        IHostedConversationClient client = conversationClient.AsIHostedConversationClient();

        Assert.Same(conversationClient, client.GetService(typeof(ConversationClient)));
    }

    [Fact]
    public void GetService_ReturnsSelf()
    {
        ConversationClient conversationClient = new(new ApiKeyCredential("key"));

        IHostedConversationClient client = conversationClient.AsIHostedConversationClient();

        Assert.Same(client, client.GetService(typeof(IHostedConversationClient)));
    }

    [Fact]
    public void GetService_ReturnsNull_ForUnknownType()
    {
        ConversationClient conversationClient = new(new ApiKeyCredential("key"));

        IHostedConversationClient client = conversationClient.AsIHostedConversationClient();

        Assert.Null(client.GetService(typeof(string)));
    }

    [Fact]
    public void ListConversationsAsync_ThrowsNotSupportedException()
    {
        ConversationClient conversationClient = new(new ApiKeyCredential("key"));

        IHostedConversationClient client = conversationClient.AsIHostedConversationClient();

        var ex = Assert.Throws<NotSupportedException>(() => client.ListConversationsAsync());
        Assert.Contains("does not currently support listing conversations", ex.Message);
    }
}
