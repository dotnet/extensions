// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedConversationTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        HostedConversation conversation = new();
        Assert.Null(conversation.ConversationId);
        Assert.Null(conversation.CreatedAt);
        Assert.Null(conversation.Metadata);
        Assert.Null(conversation.RawRepresentation);
        Assert.Null(conversation.AdditionalProperties);
    }

    [Fact]
    public void ConversationId_Roundtrips()
    {
        HostedConversation conversation = new();
        Assert.Null(conversation.ConversationId);

        conversation.ConversationId = "conv-123";
        Assert.Equal("conv-123", conversation.ConversationId);

        conversation.ConversationId = null;
        Assert.Null(conversation.ConversationId);
    }

    [Fact]
    public void CreatedAt_Roundtrips()
    {
        HostedConversation conversation = new();
        Assert.Null(conversation.CreatedAt);

        DateTimeOffset createdAt = new(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        conversation.CreatedAt = createdAt;
        Assert.Equal(createdAt, conversation.CreatedAt);

        conversation.CreatedAt = null;
        Assert.Null(conversation.CreatedAt);
    }

    [Fact]
    public void Metadata_Roundtrips()
    {
        HostedConversation conversation = new();
        Assert.Null(conversation.Metadata);

        AdditionalPropertiesDictionary<string> metadata = new()
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
        };
        conversation.Metadata = metadata;
        Assert.Same(metadata, conversation.Metadata);

        conversation.Metadata = null;
        Assert.Null(conversation.Metadata);
    }

    [Fact]
    public void RawRepresentation_Roundtrips()
    {
        HostedConversation conversation = new();
        Assert.Null(conversation.RawRepresentation);

        object raw = new();
        conversation.RawRepresentation = raw;
        Assert.Same(raw, conversation.RawRepresentation);

        conversation.RawRepresentation = null;
        Assert.Null(conversation.RawRepresentation);
    }

    [Fact]
    public void AdditionalProperties_Roundtrips()
    {
        HostedConversation conversation = new();
        Assert.Null(conversation.AdditionalProperties);

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };
        conversation.AdditionalProperties = additionalProps;
        Assert.Same(additionalProps, conversation.AdditionalProperties);

        conversation.AdditionalProperties = null;
        Assert.Null(conversation.AdditionalProperties);
    }
}
