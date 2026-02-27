// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ShellCallContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        ShellCallContent content = new("call123", "shell");
        Assert.Equal("call123", content.CallId);
        Assert.Equal("shell", content.Name);
        Assert.Null(content.Arguments);
        Assert.Null(content.Commands);
        Assert.Null(content.TimeoutMs);
        Assert.Null(content.MaxOutputLength);
        Assert.Null(content.Status);
    }

    [Fact]
    public void Constructor_WithArguments()
    {
        var args = new Dictionary<string, object?> { ["command"] = "ls" };
        ShellCallContent content = new("call123", "local_shell", args);

        Assert.Equal("call123", content.CallId);
        Assert.Equal("local_shell", content.Name);
        Assert.Same(args, content.Arguments);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ShellCallContent content = new("call123", "shell");

        Assert.Null(content.Commands);
        IList<string> commands = ["ls -la", "pwd"];
        content.Commands = commands;
        Assert.Same(commands, content.Commands);

        Assert.Null(content.TimeoutMs);
        content.TimeoutMs = 120000;
        Assert.Equal(120000, content.TimeoutMs);

        Assert.Null(content.MaxOutputLength);
        content.MaxOutputLength = 4096;
        Assert.Equal(4096, content.MaxOutputLength);

        Assert.Null(content.Status);
        content.Status = "completed";
        Assert.Equal("completed", content.Status);

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
    public void Serialization_Roundtrips()
    {
        ShellCallContent content = new("call123", "shell")
        {
            Commands = ["ls -la", "pwd"],
            TimeoutMs = 60000,
            MaxOutputLength = 4096,
            Status = "completed",
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<ShellCallContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("call123", deserializedSut.CallId);
        Assert.Equal("shell", deserializedSut.Name);
        Assert.NotNull(deserializedSut.Commands);
        Assert.Equal(2, deserializedSut.Commands.Count);
        Assert.Equal("ls -la", deserializedSut.Commands[0]);
        Assert.Equal("pwd", deserializedSut.Commands[1]);
        Assert.Equal(60000, deserializedSut.TimeoutMs);
        Assert.Equal(4096, deserializedSut.MaxOutputLength);
        Assert.Equal("completed", deserializedSut.Status);
    }

    [Fact]
    public void Serialization_PolymorphicAsAIContent_Roundtrips()
    {
        AIContent content = new ShellCallContent("call123", "shell")
        {
            Commands = ["echo hello"],
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        Assert.Contains("\"$type\"", json);
        Assert.Contains("\"shellCall\"", json);

        var deserializedSut = JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions);
        var shellCall = Assert.IsType<ShellCallContent>(deserializedSut);
        Assert.Equal("call123", shellCall.CallId);
        Assert.Equal("shell", shellCall.Name);
        Assert.NotNull(shellCall.Commands);
        Assert.Single(shellCall.Commands);
        Assert.Equal("echo hello", shellCall.Commands[0]);
    }
}
