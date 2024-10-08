// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatRoleTests
{
    [Fact]
    public void Constructor_Value_Roundtrips()
    {
        Assert.Equal("abc", new ChatRole("abc").Value);
    }

    [Fact]
    public void Constructor_NullOrWhiteSpace_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ChatRole(null!));
        Assert.Throws<ArgumentException>(() => new ChatRole("  "));
    }

    [Fact]
    public void Equality_UsesOrdinalIgnoreCaseComparison()
    {
        Assert.True(new ChatRole("abc").Equals(new ChatRole("ABC")));
        Assert.True(new ChatRole("abc").Equals((object)new ChatRole("ABC")));
        Assert.True(new ChatRole("abc") == new ChatRole("ABC"));
        Assert.False(new ChatRole("abc") != new ChatRole("ABC"));

        Assert.False(new ChatRole("abc").Equals(new ChatRole("def")));
        Assert.False(new ChatRole("abc").Equals((object)new ChatRole("def")));
        Assert.False(new ChatRole("abc").Equals(null));
        Assert.False(new ChatRole("abc").Equals("abc"));
        Assert.False(new ChatRole("abc") == new ChatRole("def"));
        Assert.True(new ChatRole("abc") != new ChatRole("def"));

        Assert.Equal(new ChatRole("abc").GetHashCode(), new ChatRole("abc").GetHashCode());
        Assert.Equal(new ChatRole("abc").GetHashCode(), new ChatRole("ABC").GetHashCode());
        Assert.NotEqual(new ChatRole("abc").GetHashCode(), new ChatRole("def").GetHashCode()); // not guaranteed
    }

    [Fact]
    public void Singletons_UseKnownValues()
    {
        Assert.Equal("assistant", ChatRole.Assistant.Value);
        Assert.Equal("system", ChatRole.System.Value);
        Assert.Equal("tool", ChatRole.Tool.Value);
        Assert.Equal("user", ChatRole.User.Value);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        ChatRole role = new("abc");
        string? json = JsonSerializer.Serialize(role, TestJsonSerializerContext.Default.ChatRole);
        Assert.Equal("\"abc\"", json);

        ChatRole? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatRole);
        Assert.Equal(role, result);
    }
}
