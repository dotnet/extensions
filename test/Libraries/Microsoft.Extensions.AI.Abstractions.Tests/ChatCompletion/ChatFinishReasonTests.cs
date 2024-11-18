// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatFinishReasonTests
{
    [Fact]
    public void Constructor_Value_Roundtrips()
    {
        Assert.Equal("abc", new ChatFinishReason("abc").Value);
    }

    [Fact]
    public void Constructor_NullOrWhiteSpace_Throws()
    {
        Assert.Throws<ArgumentNullException>("value", () => new ChatFinishReason(null!));
        Assert.Throws<ArgumentException>("value", () => new ChatFinishReason("  "));
    }

    [Fact]
    public void Equality_UsesOrdinalIgnoreCaseComparison()
    {
        Assert.True(new ChatFinishReason("abc").Equals(new ChatFinishReason("ABC")));
        Assert.True(new ChatFinishReason("abc").Equals((object)new ChatFinishReason("ABC")));
        Assert.True(new ChatFinishReason("abc") == new ChatFinishReason("ABC"));
        Assert.Equal(new ChatFinishReason("abc").GetHashCode(), new ChatFinishReason("ABC").GetHashCode());
        Assert.False(new ChatFinishReason("abc") != new ChatFinishReason("ABC"));

        Assert.False(new ChatFinishReason("abc").Equals(new ChatFinishReason("def")));
        Assert.False(new ChatFinishReason("abc").Equals((object)new ChatFinishReason("def")));
        Assert.False(new ChatFinishReason("abc").Equals(null));
        Assert.False(new ChatFinishReason("abc").Equals("abc"));
        Assert.False(new ChatFinishReason("abc") == new ChatFinishReason("def"));
        Assert.True(new ChatFinishReason("abc") != new ChatFinishReason("def"));
        Assert.NotEqual(new ChatFinishReason("abc").GetHashCode(), new ChatFinishReason("def").GetHashCode()); // not guaranteed due to possible hash code collisions
    }

    [Fact]
    public void Singletons_UseKnownValues()
    {
        Assert.Equal("stop", ChatFinishReason.Stop.Value);
        Assert.Equal("length", ChatFinishReason.Length.Value);
        Assert.Equal("tool_calls", ChatFinishReason.ToolCalls.Value);
        Assert.Equal("content_filter", ChatFinishReason.ContentFilter.Value);
    }

    [Fact]
    public void Value_NormalizesToStopped()
    {
        Assert.Equal("test", new ChatFinishReason("test").Value);
        Assert.Equal("test", new ChatFinishReason("test").ToString());

        Assert.Equal("TEST", new ChatFinishReason("TEST").Value);
        Assert.Equal("TEST", new ChatFinishReason("TEST").ToString());

        Assert.Equal("stop", default(ChatFinishReason).Value);
        Assert.Equal("stop", default(ChatFinishReason).ToString());
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        ChatFinishReason role = new("abc");
        string? json = JsonSerializer.Serialize(role, TestJsonSerializerContext.Default.ChatFinishReason);
        Assert.Equal("\"abc\"", json);

        ChatFinishReason? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatFinishReason);
        Assert.Equal(role, result);
    }
}
