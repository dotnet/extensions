// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ShellCommandOutputTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        ShellCommandOutput output = new();
        Assert.Null(output.Stdout);
        Assert.Null(output.Stderr);
        Assert.Null(output.ExitCode);
        Assert.False(output.TimedOut);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ShellCommandOutput output = new();

        Assert.Null(output.Stdout);
        output.Stdout = "hello";
        Assert.Equal("hello", output.Stdout);

        Assert.Null(output.Stderr);
        output.Stderr = "error message";
        Assert.Equal("error message", output.Stderr);

        Assert.Null(output.ExitCode);
        output.ExitCode = 42;
        Assert.Equal(42, output.ExitCode);

        Assert.False(output.TimedOut);
        output.TimedOut = true;
        Assert.True(output.TimedOut);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        ShellCommandOutput output = new()
        {
            Stdout = "hello world",
            Stderr = "warning: something",
            ExitCode = 0,
            TimedOut = false,
        };

        var json = JsonSerializer.Serialize(output, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<ShellCommandOutput>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("hello world", deserializedSut.Stdout);
        Assert.Equal("warning: something", deserializedSut.Stderr);
        Assert.Equal(0, deserializedSut.ExitCode);
        Assert.False(deserializedSut.TimedOut);
    }

    [Fact]
    public void Serialization_TimedOut_Roundtrips()
    {
        ShellCommandOutput output = new()
        {
            Stdout = "partial output",
            TimedOut = true,
        };

        var json = JsonSerializer.Serialize(output, AIJsonUtilities.DefaultOptions);
        var deserializedSut = JsonSerializer.Deserialize<ShellCommandOutput>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedSut);
        Assert.Equal("partial output", deserializedSut.Stdout);
        Assert.Null(deserializedSut.ExitCode);
        Assert.True(deserializedSut.TimedOut);
    }
}
