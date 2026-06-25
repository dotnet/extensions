// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedFileClientOptionsTests
{
    [Fact]
    public void PropsDefault()
    {
        var options = new HostedFileClientOptions();
        Assert.Null(options.Purpose);
        Assert.Null(options.Limit);
        Assert.Null(options.Scope);
        Assert.Null(options.RawRepresentationFactory);
        Assert.Null(options.AdditionalProperties);

        HostedFileClientOptions clone = options.Clone();
        Assert.Null(clone.Purpose);
        Assert.Null(clone.Limit);
        Assert.Null(clone.Scope);
        Assert.Null(clone.RawRepresentationFactory);
        Assert.Null(clone.AdditionalProperties);
    }

    [Fact]
    public void PropsRoundtrip()
    {
        var props = new AdditionalPropertiesDictionary { { "key", "value" } };
        Func<IHostedFileClient, object?> factory = _ => "raw";
        var options = new HostedFileClientOptions
        {
            Purpose = "fine-tune",
            Limit = 50,
            Scope = "container-1",
            RawRepresentationFactory = factory,
            AdditionalProperties = props
        };

        Assert.Equal("fine-tune", options.Purpose);
        Assert.Equal(50, options.Limit);
        Assert.Equal("container-1", options.Scope);
        Assert.Same(factory, options.RawRepresentationFactory);
        Assert.Same(props, options.AdditionalProperties);

        HostedFileClientOptions clone = options.Clone();
        Assert.Equal("fine-tune", clone.Purpose);
        Assert.Equal(50, clone.Limit);
        Assert.Equal("container-1", clone.Scope);
        Assert.Same(factory, clone.RawRepresentationFactory);
        Assert.Equal(props, clone.AdditionalProperties);
    }

    [Fact]
    public void CopyConstructors_EnableHierarchyCloning()
    {
        OptionsB b = new()
        {
            Purpose = "test",
            A = 42,
            B = 84,
        };

        HostedFileClientOptions clone = b.Clone();

        Assert.Equal("test", clone.Purpose);
        Assert.Equal(42, Assert.IsType<OptionsA>(clone, exactMatch: false).A);
        Assert.Equal(84, Assert.IsType<OptionsB>(clone, exactMatch: true).B);
    }

    private class OptionsA : HostedFileClientOptions
    {
        public OptionsA()
        {
        }

        protected OptionsA(OptionsA other)
            : base(other)
        {
            A = other.A;
        }

        public int A { get; set; }

        public override HostedFileClientOptions Clone() => new OptionsA(this);
    }

    private class OptionsB : OptionsA
    {
        public OptionsB()
        {
        }

        protected OptionsB(OptionsB other)
            : base(other)
        {
            B = other.B;
        }

        public int B { get; set; }

        public override HostedFileClientOptions Clone() => new OptionsB(this);
    }

    [Fact]
    public void CopyConstructor_Null_Valid()
    {
        PassedNullToBaseOptions options = new();
        Assert.NotNull(options);
    }

    private class PassedNullToBaseOptions : HostedFileClientOptions
    {
        public PassedNullToBaseOptions()
            : base(null)
        {
        }
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        HostedFileClientOptions options = new()
        {
            Purpose = "fine-tune",
            Limit = 50,
            Scope = "container-1",
            AdditionalProperties = new() { { "key", "value" } },
        };

        string json = JsonSerializer.Serialize(options, TestJsonSerializerContext.Default.HostedFileClientOptions);

        HostedFileClientOptions? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.HostedFileClientOptions);
        Assert.NotNull(deserialized);

        Assert.Equal("fine-tune", deserialized.Purpose);
        Assert.Equal(50, deserialized.Limit);
        Assert.Equal("container-1", deserialized.Scope);
        Assert.Null(deserialized.RawRepresentationFactory);
        Assert.NotNull(deserialized.AdditionalProperties);
        Assert.Equal("value", deserialized.AdditionalProperties["key"]?.ToString());
    }
}
