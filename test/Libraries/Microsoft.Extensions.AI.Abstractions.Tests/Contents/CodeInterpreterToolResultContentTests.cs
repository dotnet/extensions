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
        CodeInterpreterToolResultContent c = new();
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Null(c.CallId);
        Assert.Null(c.Output);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        CodeInterpreterToolResultContent c = new();

        Assert.Null(c.CallId);
        c.CallId = "call123";
        Assert.Equal("call123", c.CallId);

        Assert.Null(c.Output);
        IList<AIContent> output = [new TextContent("Hello, World!")];
        c.Output = output;
        Assert.Same(output, c.Output);

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
        CodeInterpreterToolResultContent c = new()
        {
            CallId = "call789",
            Output =
            [
                new TextContent("Execution completed"),
                new HostedFileContent("output.png"),
                new DataContent(new byte[] { 1, 2, 3 }, "application/octet-stream"),
                new ErrorContent("Warning: deprecated function")
            ]
        };

        Assert.NotNull(c.Output);
        Assert.Equal(4, c.Output.Count);
        Assert.IsType<TextContent>(c.Output[0]);
        Assert.IsType<HostedFileContent>(c.Output[1]);
        Assert.IsType<DataContent>(c.Output[2]);
        Assert.IsType<ErrorContent>(c.Output[3]);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        CodeInterpreterToolResultContent content = new()
        {
            CallId = "call123",
            Output =
            [
                new TextContent("Hello, World!"),
                new HostedFileContent("result.txt")
            ]
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<CodeInterpreterToolResultContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("call123", deserializedSut.CallId);
        Assert.NotNull(deserializedSut.Output);
        Assert.Equal(2, deserializedSut.Output.Count);
        Assert.IsType<TextContent>(deserializedSut.Output[0]);
        Assert.Equal("Hello, World!", ((TextContent)deserializedSut.Output[0]).Text);
        Assert.IsType<HostedFileContent>(deserializedSut.Output[1]);
        Assert.Equal("result.txt", ((HostedFileContent)deserializedSut.Output[1]).FileId);
    }
}
