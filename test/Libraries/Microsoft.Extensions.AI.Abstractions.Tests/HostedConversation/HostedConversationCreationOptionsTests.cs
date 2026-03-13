// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedConversationCreationOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        HostedConversationCreationOptions options = new();
        Assert.Null(options.Metadata);
        Assert.Null(options.Messages);
        Assert.Null(options.RawRepresentationFactory);
        Assert.Null(options.AdditionalProperties);

        HostedConversationCreationOptions clone = options.Clone();
        Assert.Null(clone.Metadata);
        Assert.Null(clone.Messages);
        Assert.Null(clone.RawRepresentationFactory);
        Assert.Null(clone.AdditionalProperties);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        HostedConversationCreationOptions options = new();

        AdditionalPropertiesDictionary<string> metadata = new()
        {
            ["key1"] = "value1",
        };

        List<ChatMessage> messages =
        [
            new(ChatRole.User, "Hello"),
            new(ChatRole.Assistant, "Hi there"),
        ];

        Func<IHostedConversationClient, object?> rawRepresentationFactory = (c) => null;

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        options.Metadata = metadata;
        options.Messages = messages;
        options.RawRepresentationFactory = rawRepresentationFactory;
        options.AdditionalProperties = additionalProps;

        Assert.Same(metadata, options.Metadata);
        Assert.Same(messages, options.Messages);
        Assert.Same(rawRepresentationFactory, options.RawRepresentationFactory);
        Assert.Same(additionalProps, options.AdditionalProperties);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        HostedConversationCreationOptions original = new()
        {
            Metadata = new() { ["key1"] = "value1" },
            Messages = [new(ChatRole.User, "Hello")],
            RawRepresentationFactory = (c) => null,
            AdditionalProperties = new() { ["prop"] = "val" },
        };

        HostedConversationCreationOptions clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.NotNull(clone.Metadata);
        Assert.NotNull(clone.Messages);
        Assert.NotNull(clone.RawRepresentationFactory);
        Assert.NotNull(clone.AdditionalProperties);
    }

    [Fact]
    public void Clone_DeepCopiesMetadata()
    {
        HostedConversationCreationOptions original = new()
        {
            Metadata = new() { ["key1"] = "value1" },
        };

        HostedConversationCreationOptions clone = original.Clone();

        Assert.NotSame(original.Metadata, clone.Metadata);
        Assert.Equal("value1", clone.Metadata!["key1"]);

        // Modifying clone should not affect original
        clone.Metadata["key1"] = "modified";
        Assert.Equal("value1", original.Metadata["key1"]);

        clone.Metadata["key2"] = "newvalue";
        Assert.False(original.Metadata.ContainsKey("key2"));
    }

    [Fact]
    public void Clone_DeepCopiesMessages()
    {
        List<ChatMessage> messages =
        [
            new(ChatRole.User, "Hello"),
            new(ChatRole.Assistant, "Hi"),
        ];

        HostedConversationCreationOptions original = new()
        {
            Messages = messages,
        };

        HostedConversationCreationOptions clone = original.Clone();

        Assert.NotSame(original.Messages, clone.Messages);
        Assert.Equal(2, clone.Messages!.Count);

        // Adding to clone should not affect original
        clone.Messages.Add(new(ChatRole.User, "Another message"));
        Assert.Equal(2, original.Messages.Count);
    }

    [Fact]
    public void Clone_CopiesRawRepresentationFactoryByReference()
    {
        Func<IHostedConversationClient, object?> factory = (c) => "test";

        HostedConversationCreationOptions original = new()
        {
            RawRepresentationFactory = factory,
        };

        HostedConversationCreationOptions clone = original.Clone();

        Assert.Same(original.RawRepresentationFactory, clone.RawRepresentationFactory);
    }

    [Fact]
    public void Clone_DeepCopiesAdditionalProperties()
    {
        HostedConversationCreationOptions original = new()
        {
            AdditionalProperties = new() { ["key"] = "value" },
        };

        HostedConversationCreationOptions clone = original.Clone();

        Assert.NotSame(original.AdditionalProperties, clone.AdditionalProperties);
        Assert.Equal("value", clone.AdditionalProperties!["key"]);

        // Modifying clone should not affect original
        clone.AdditionalProperties["key"] = "modified";
        Assert.Equal("value", original.AdditionalProperties["key"]);

        clone.AdditionalProperties["newkey"] = "newval";
        Assert.False(original.AdditionalProperties.ContainsKey("newkey"));
    }

    [Fact]
    public void CopyConstructor_Null_Valid()
    {
        PassedNullToBaseOptions options = new();
        Assert.NotNull(options);
    }

    [Fact]
    public void CopyConstructors_EnableHierarchyCloning()
    {
        DerivedOptions derived = new()
        {
            Metadata = new() { ["key"] = "value" },
            CustomProperty = 42,
        };

        HostedConversationCreationOptions clone = derived.Clone();

        Assert.NotNull(clone.Metadata);
        Assert.Equal("value", clone.Metadata["key"]);
        Assert.Equal(42, Assert.IsType<DerivedOptions>(clone).CustomProperty);
    }

    private class PassedNullToBaseOptions : HostedConversationCreationOptions
    {
        public PassedNullToBaseOptions()
            : base(null)
        {
        }
    }

    private class DerivedOptions : HostedConversationCreationOptions
    {
        public DerivedOptions()
        {
        }

        protected DerivedOptions(DerivedOptions other)
            : base(other)
        {
            CustomProperty = other.CustomProperty;
        }

        public int CustomProperty { get; set; }

        public override HostedConversationCreationOptions Clone() => new DerivedOptions(this);
    }
}
