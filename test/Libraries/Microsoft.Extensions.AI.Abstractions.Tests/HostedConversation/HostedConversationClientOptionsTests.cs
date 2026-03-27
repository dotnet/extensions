// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedConversationClientOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        HostedConversationClientOptions options = new();
        Assert.Null(options.Limit);
        Assert.Null(options.RawRepresentationFactory);
        Assert.Null(options.AdditionalProperties);

        HostedConversationClientOptions clone = options.Clone();
        Assert.Null(clone.Limit);
        Assert.Null(clone.RawRepresentationFactory);
        Assert.Null(clone.AdditionalProperties);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        HostedConversationClientOptions options = new();

        Func<IHostedConversationClient, object?> rawRepresentationFactory = (c) => null;

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        options.Limit = 42;
        options.RawRepresentationFactory = rawRepresentationFactory;
        options.AdditionalProperties = additionalProps;

        Assert.Equal(42, options.Limit);
        Assert.Same(rawRepresentationFactory, options.RawRepresentationFactory);
        Assert.Same(additionalProps, options.AdditionalProperties);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        HostedConversationClientOptions original = new()
        {
            Limit = 10,
            RawRepresentationFactory = (c) => null,
            AdditionalProperties = new() { ["prop"] = "val" },
        };

        HostedConversationClientOptions clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal(10, clone.Limit);
        Assert.NotNull(clone.RawRepresentationFactory);
        Assert.NotNull(clone.AdditionalProperties);
    }

    [Fact]
    public void Clone_CopiesRawRepresentationFactoryByReference()
    {
        Func<IHostedConversationClient, object?> factory = (c) => "test";

        HostedConversationClientOptions original = new()
        {
            RawRepresentationFactory = factory,
        };

        HostedConversationClientOptions clone = original.Clone();

        Assert.Same(original.RawRepresentationFactory, clone.RawRepresentationFactory);
    }

    [Fact]
    public void Clone_DeepCopiesAdditionalProperties()
    {
        HostedConversationClientOptions original = new()
        {
            AdditionalProperties = new() { ["key"] = "value" },
        };

        HostedConversationClientOptions clone = original.Clone();

        Assert.NotSame(original.AdditionalProperties, clone.AdditionalProperties);
        Assert.Equal("value", clone.AdditionalProperties!["key"]);

        // Modifying clone should not affect original
        clone.AdditionalProperties["key"] = "modified";
        Assert.Equal("value", original.AdditionalProperties["key"]);

        clone.AdditionalProperties["newkey"] = "newval";
        Assert.False(original.AdditionalProperties.ContainsKey("newkey"));
    }

    [Fact]
    public void Clone_CopiesLimit()
    {
        HostedConversationClientOptions original = new()
        {
            Limit = 25,
        };

        HostedConversationClientOptions clone = original.Clone();

        Assert.Equal(25, clone.Limit);

        // Modifying clone should not affect original
        clone.Limit = 50;
        Assert.Equal(25, original.Limit);
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
            Limit = 5,
            CustomProperty = 42,
        };

        HostedConversationClientOptions clone = derived.Clone();

        Assert.Equal(5, clone.Limit);
        Assert.Equal(42, Assert.IsType<DerivedOptions>(clone).CustomProperty);
    }

    private class PassedNullToBaseOptions : HostedConversationClientOptions
    {
        public PassedNullToBaseOptions()
            : base(null)
        {
        }
    }

    private class DerivedOptions : HostedConversationClientOptions
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

        public override HostedConversationClientOptions Clone() => new DerivedOptions(this);
    }
}
