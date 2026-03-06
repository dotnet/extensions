// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ShellCallContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        ShellCallContent content = new("call123");
        Assert.Equal("call123", content.CallId);
        Assert.Null(content.Commands);
        Assert.Null(content.Timeout);
        Assert.Null(content.MaxOutputLength);
        Assert.Null(content.Status);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ShellCallContent content = new("call123");

        Assert.Null(content.Commands);
        IList<string> commands = ["ls -la", "pwd"];
        content.Commands = commands;
        Assert.Same(commands, content.Commands);

        Assert.Null(content.Timeout);
        content.Timeout = TimeSpan.FromMinutes(2);
        Assert.Equal(TimeSpan.FromMinutes(2), content.Timeout);

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
        ShellCallContent content = new("call123")
        {
            Commands = ["ls -la", "pwd"],
            Timeout = TimeSpan.FromSeconds(60),
            MaxOutputLength = 4096,
            Status = "completed",
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<ShellCallContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("call123", deserializedSut.CallId);
        Assert.NotNull(deserializedSut.Commands);
        Assert.Equal(2, deserializedSut.Commands.Count);
        Assert.Equal("ls -la", deserializedSut.Commands[0]);
        Assert.Equal("pwd", deserializedSut.Commands[1]);
        Assert.Equal(TimeSpan.FromSeconds(60), deserializedSut.Timeout);
        Assert.Equal(4096, deserializedSut.MaxOutputLength);
        Assert.Equal("completed", deserializedSut.Status);
    }

    [Fact]
    public void Serialization_PolymorphicAsAIContent_Roundtrips()
    {
        AIContent content = new ShellCallContent("call123")
        {
            Commands = ["echo hello"],
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        Assert.Contains("\"$type\"", json);
        Assert.Contains("\"shellCall\"", json);

        var deserializedSut = JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions);
        var shellCall = Assert.IsType<ShellCallContent>(deserializedSut);
        Assert.Equal("call123", shellCall.CallId);
        Assert.NotNull(shellCall.Commands);
        Assert.Single(shellCall.Commands);
        Assert.Equal("echo hello", shellCall.Commands[0]);
    }
}
