// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class CodeInterpreterToolCallContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        CodeInterpreterToolCallContent c = new("callId1");
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal("callId1", c.CallId);
        Assert.Null(c.Inputs);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        CodeInterpreterToolCallContent c = new("call123");

        Assert.Equal("call123", c.CallId);

        Assert.Null(c.Inputs);
        IList<AIContent> inputs = [new TextContent("print('hello')")];
        c.Inputs = inputs;
        Assert.Same(inputs, c.Inputs);

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);
    }

    [Fact]
    public void Inputs_SupportsMultipleContentTypes()
    {
        CodeInterpreterToolCallContent c = new("call456")
        {
            Inputs =
            [
                new TextContent("import numpy as np"),
                new HostedFileContent("file123"),
                new DataContent(new byte[] { 1, 2, 3 }, "application/octet-stream")
            ]
        };

        Assert.NotNull(c.Inputs);
        Assert.Equal(3, c.Inputs.Count);
        Assert.IsType<TextContent>(c.Inputs[0]);
        Assert.IsType<HostedFileContent>(c.Inputs[1]);
        Assert.IsType<DataContent>(c.Inputs[2]);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        CodeInterpreterToolCallContent content = new("call123")
        {
            Inputs =
            [
                new TextContent("print('hello')"),
                new HostedFileContent("file456")
            ]
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<CodeInterpreterToolCallContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("call123", deserializedSut.CallId);
        Assert.NotNull(deserializedSut.Inputs);
        Assert.Equal(2, deserializedSut.Inputs.Count);
        Assert.IsType<TextContent>(deserializedSut.Inputs[0]);
        Assert.Equal("print('hello')", ((TextContent)deserializedSut.Inputs[0]).Text);
        Assert.IsType<HostedFileContent>(deserializedSut.Inputs[1]);
        Assert.Equal("file456", ((HostedFileContent)deserializedSut.Inputs[1]).FileId);
    }
}
