// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class CodeInterpreterToolResultContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        CodeInterpreterToolResultContent c = new("callId1");
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal("callId1", c.CallId);
        Assert.Null(c.Outputs);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        CodeInterpreterToolResultContent c = new("call123");

        Assert.Equal("call123", c.CallId);

        Assert.Null(c.Outputs);
        IList<AIContent> output = [new TextContent("Hello, World!")];
        c.Outputs = output;
        Assert.Same(output, c.Outputs);

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
    public void Output_SupportsMultipleContentTypes()
    {
        CodeInterpreterToolResultContent c = new("call789")
        {
            Outputs =
            [
                new TextContent("Execution completed"),
                new HostedFileContent("output.png"),
                new DataContent(new byte[] { 1, 2, 3 }, "application/octet-stream"),
                new ErrorContent("Warning: deprecated function")
            ]
        };

        Assert.NotNull(c.Outputs);
        Assert.Equal(4, c.Outputs.Count);
        Assert.IsType<TextContent>(c.Outputs[0]);
        Assert.IsType<HostedFileContent>(c.Outputs[1]);
        Assert.IsType<DataContent>(c.Outputs[2]);
        Assert.IsType<ErrorContent>(c.Outputs[3]);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        CodeInterpreterToolResultContent content = new("call123")
        {
            Outputs =
            [
                new TextContent("Hello, World!"),
                new HostedFileContent("result.txt")
            ]
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<CodeInterpreterToolResultContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("call123", deserializedSut.CallId);
        Assert.NotNull(deserializedSut.Outputs);
        Assert.Equal(2, deserializedSut.Outputs.Count);
        Assert.IsType<TextContent>(deserializedSut.Outputs[0]);
        Assert.Equal("Hello, World!", ((TextContent)deserializedSut.Outputs[0]).Text);
        Assert.IsType<HostedFileContent>(deserializedSut.Outputs[1]);
        Assert.Equal("result.txt", ((HostedFileContent)deserializedSut.Outputs[1]).FileId);
    }
}
