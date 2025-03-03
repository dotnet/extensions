// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatToolModeTests
{
    [Fact]
    public void Singletons_Idempotent()
    {
        Assert.Same(ChatToolMode.Auto, ChatToolMode.Auto);
        Assert.Same(ChatToolMode.None, ChatToolMode.None);
        Assert.Same(ChatToolMode.RequireAny, ChatToolMode.RequireAny);
    }

    [Fact]
    public void Equality_ComparersProduceExpectedResults()
    {
        Assert.True(ChatToolMode.Auto == ChatToolMode.Auto);
        Assert.True(ChatToolMode.Auto.Equals(ChatToolMode.Auto));
        Assert.False(ChatToolMode.Auto.Equals(ChatToolMode.RequireAny));
        Assert.False(ChatToolMode.Auto.Equals(new RequiredChatToolMode(null)));
        Assert.False(ChatToolMode.Auto.Equals(new RequiredChatToolMode("func")));
        Assert.Equal(ChatToolMode.Auto.GetHashCode(), ChatToolMode.Auto.GetHashCode());

        Assert.True(ChatToolMode.None == ChatToolMode.None);
        Assert.True(ChatToolMode.None.Equals(ChatToolMode.None));
        Assert.False(ChatToolMode.None.Equals(ChatToolMode.RequireAny));
        Assert.False(ChatToolMode.None.Equals(new RequiredChatToolMode(null)));
        Assert.False(ChatToolMode.None.Equals(new RequiredChatToolMode("func")));
        Assert.Equal(ChatToolMode.None.GetHashCode(), ChatToolMode.None.GetHashCode());

        Assert.True(ChatToolMode.RequireAny == ChatToolMode.RequireAny);
        Assert.True(ChatToolMode.RequireAny.Equals(ChatToolMode.RequireAny));
        Assert.False(ChatToolMode.RequireAny.Equals(ChatToolMode.Auto));
        Assert.False(ChatToolMode.RequireAny.Equals(new RequiredChatToolMode("func")));

        Assert.True(ChatToolMode.RequireAny.Equals(new RequiredChatToolMode(null)));
        Assert.Equal(ChatToolMode.RequireAny.GetHashCode(), new RequiredChatToolMode(null).GetHashCode());
        Assert.Equal(ChatToolMode.RequireAny.GetHashCode(), ChatToolMode.RequireAny.GetHashCode());

        Assert.True(new RequiredChatToolMode("func").Equals(new RequiredChatToolMode("func")));
        Assert.Equal(new RequiredChatToolMode("func").GetHashCode(), new RequiredChatToolMode("func").GetHashCode());

        Assert.False(new RequiredChatToolMode("func1").Equals(new RequiredChatToolMode("func2")));
        Assert.NotEqual(new RequiredChatToolMode("func1").GetHashCode(), new RequiredChatToolMode("func2").GetHashCode()); // technically not guaranteed

        Assert.False(new RequiredChatToolMode("func1").Equals(new RequiredChatToolMode("FUNC1")));
        Assert.NotEqual(new RequiredChatToolMode("func1").GetHashCode(), new RequiredChatToolMode("FUNC1").GetHashCode()); // technically not guaranteed
    }

    [Fact]
    public void Serialization_AutoRoundtrips()
    {
        string json = JsonSerializer.Serialize(ChatToolMode.Auto, TestJsonSerializerContext.Default.ChatToolMode);
        Assert.Equal("""{"$type":"auto"}""", json);

        ChatToolMode? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatToolMode);
        Assert.Equal(ChatToolMode.Auto, result);
    }

    [Fact]
    public void Serialization_NoneRoundtrips()
    {
        string json = JsonSerializer.Serialize(ChatToolMode.None, TestJsonSerializerContext.Default.ChatToolMode);
        Assert.Equal("""{"$type":"none"}""", json);

        ChatToolMode? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatToolMode);
        Assert.Equal(ChatToolMode.None, result);
    }

    [Fact]
    public void Serialization_RequireAnyRoundtrips()
    {
        string json = JsonSerializer.Serialize(ChatToolMode.RequireAny, TestJsonSerializerContext.Default.ChatToolMode);
        Assert.Equal("""{"$type":"required"}""", json);

        ChatToolMode? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatToolMode);
        Assert.Equal(ChatToolMode.RequireAny, result);
    }

    [Fact]
    public void Serialization_RequireSpecificRoundtrips()
    {
        string json = JsonSerializer.Serialize(ChatToolMode.RequireSpecific("myFunc"), TestJsonSerializerContext.Default.ChatToolMode);
        Assert.Equal("""{"$type":"required","requiredFunctionName":"myFunc"}""", json);

        ChatToolMode? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatToolMode);
        Assert.Equal(ChatToolMode.RequireSpecific("myFunc"), result);
    }
}
