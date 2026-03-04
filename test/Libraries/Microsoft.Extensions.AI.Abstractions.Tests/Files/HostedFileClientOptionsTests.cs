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
