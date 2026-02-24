// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ShellResultContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        ShellResultContent content = new("call123");
        Assert.Equal("call123", content.CallId);
        Assert.Null(content.Result);
        Assert.Null(content.Output);
        Assert.Null(content.MaxOutputLength);
    }

    [Fact]
    public void Constructor_WithResult()
    {
        ShellResultContent content = new("call123", "some result");
        Assert.Equal("call123", content.CallId);
        Assert.Equal("some result", content.Result);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ShellResultContent content = new("call123");

        Assert.Null(content.Output);
        IList<ShellCommandOutput> output =
        [
            new ShellCommandOutput { Stdout = "hello", ExitCode = 0 }
        ];
        content.Output = output;
        Assert.Same(output, content.Output);

        Assert.Null(content.MaxOutputLength);
        content.MaxOutputLength = 4096;
        Assert.Equal(4096, content.MaxOutputLength);

        Assert.Null(content.RawRepresentation);
        object raw = new();
        content.RawRepresentation = raw;
        Assert.Same(raw, content.RawRepresentation);

        Assert.Null(content.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        content.AdditionalProperties = props;
        Assert.Same(props, content.AdditionalProperties);
    }

    [Fact]
    public void Output_SupportsMultipleItems()
    {
        ShellResultContent content = new("call123")
        {
            Output =
            [
                new ShellCommandOutput { Stdout = "output1", ExitCode = 0 },
                new ShellCommandOutput { Stderr = "error", ExitCode = 1 },
                new ShellCommandOutput { TimedOut = true },
            ]
        };

        Assert.NotNull(content.Output);
        Assert.Equal(3, content.Output.Count);
        Assert.Equal("output1", content.Output[0].Stdout);
        Assert.Equal(0, content.Output[0].ExitCode);
        Assert.Equal("error", content.Output[1].Stderr);
        Assert.Equal(1, content.Output[1].ExitCode);
        Assert.True(content.Output[2].TimedOut);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        ShellResultContent content = new("call123")
        {
            Output =
            [
                new ShellCommandOutput { Stdout = "hello\n", ExitCode = 0 },
            ],
            MaxOutputLength = 4096,
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<ShellResultContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("call123", deserializedSut.CallId);
        Assert.NotNull(deserializedSut.Output);
        Assert.Single(deserializedSut.Output);
        Assert.Equal("hello\n", deserializedSut.Output[0].Stdout);
        Assert.Equal(0, deserializedSut.Output[0].ExitCode);
        Assert.Equal(4096, deserializedSut.MaxOutputLength);
    }

    [Fact]
    public void Serialization_PolymorphicAsAIContent_Roundtrips()
    {
        AIContent content = new ShellResultContent("call123")
        {
            Output =
            [
                new ShellCommandOutput { Stdout = "done", ExitCode = 0 },
            ],
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        Assert.Contains("\"$type\"", json);
        Assert.Contains("\"shellResult\"", json);

        var deserializedSut = JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions);
        var shellResult = Assert.IsType<ShellResultContent>(deserializedSut);
        Assert.Equal("call123", shellResult.CallId);
        Assert.NotNull(shellResult.Output);
        Assert.Single(shellResult.Output);
        Assert.Equal("done", shellResult.Output[0].Stdout);
    }
}
