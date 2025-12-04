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
        Assert.False(ChatToolMode.Auto.Equals(new RequiredChatToolMode((string?)null)));
        Assert.False(ChatToolMode.Auto.Equals(new RequiredChatToolMode("func")));
        Assert.Equal(ChatToolMode.Auto.GetHashCode(), ChatToolMode.Auto.GetHashCode());

        Assert.True(ChatToolMode.None == ChatToolMode.None);
        Assert.True(ChatToolMode.None.Equals(ChatToolMode.None));
        Assert.False(ChatToolMode.None.Equals(ChatToolMode.RequireAny));
        Assert.False(ChatToolMode.None.Equals(new RequiredChatToolMode((string?)null)));
        Assert.False(ChatToolMode.None.Equals(new RequiredChatToolMode("func")));
        Assert.Equal(ChatToolMode.None.GetHashCode(), ChatToolMode.None.GetHashCode());

        Assert.True(ChatToolMode.RequireAny == ChatToolMode.RequireAny);
        Assert.True(ChatToolMode.RequireAny.Equals(ChatToolMode.RequireAny));
        Assert.False(ChatToolMode.RequireAny.Equals(ChatToolMode.Auto));
        Assert.False(ChatToolMode.RequireAny.Equals(new RequiredChatToolMode("func")));

        Assert.True(ChatToolMode.RequireAny.Equals(new RequiredChatToolMode((string?)null)));
        Assert.Equal(ChatToolMode.RequireAny.GetHashCode(), new RequiredChatToolMode((string?)null).GetHashCode());
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

    [Fact]
    public void RequireSpecific_WithAIFunction_UsesCorrectFunctionName()
    {
        var function = AIFunctionFactory.Create(() => { }, "myFunction");

        var result = ChatToolMode.RequireSpecific(function);
        Assert.IsType<RequiredChatToolMode>(result);

        var requiredMode = Assert.IsType<RequiredChatToolMode>(result);
        Assert.Same(function, requiredMode.RequiredTool);
        Assert.Equal("myFunction", requiredMode.RequiredFunctionName);
    }

    [Fact]
    public void RequireSpecific_WithNonFunctionTool_SetsRequiredToolButNullFunctionName()
    {
        var tool = new TestNonFunctionTool("nonFunctionTool");

        var result = ChatToolMode.RequireSpecific(tool);
        Assert.IsType<RequiredChatToolMode>(result);

        var requiredMode = Assert.IsType<RequiredChatToolMode>(result);
        Assert.Same(tool, requiredMode.RequiredTool);
        Assert.Null(requiredMode.RequiredFunctionName);
    }

    [Fact]
    public void RequiredChatToolMode_Constructor_WithAITool_SetsProperties()
    {
        var tool = AIFunctionFactory.Create(() => { }, "testFunc");

        RequiredChatToolMode mode = new(tool);

        Assert.Same(tool, mode.RequiredTool);
        Assert.Equal("testFunc", mode.RequiredFunctionName);
    }

    [Fact]
    public void RequiredChatToolMode_Constructor_WithNullAITool_SetsPropertiesCorrectly()
    {
        RequiredChatToolMode mode = new((AITool?)null);

        Assert.Null(mode.RequiredTool);
        Assert.Null(mode.RequiredFunctionName);
    }

    [Fact]
    public void RequiredChatToolMode_Constructor_WithNonFunctionTool_SetsToolButNullFunctionName()
    {
        TestNonFunctionTool tool = new("nonFunc");

        RequiredChatToolMode mode = new(tool);
        Assert.Same(tool, mode.RequiredTool);

        Assert.Null(mode.RequiredFunctionName);
    }

    [Fact]
    public void RequiredChatToolMode_Equals_WithSameAITool_ReturnsTrue()
    {
        var tool = AIFunctionFactory.Create(() => { }, "testFunc");
        RequiredChatToolMode mode1 = new(tool);
        RequiredChatToolMode mode2 = new(tool);

        Assert.True(mode1.Equals(mode2));
        Assert.True(mode2.Equals(mode1));
        Assert.Equal(mode1.GetHashCode(), mode2.GetHashCode());
    }

    [Fact]
    public void RequiredChatToolMode_Equals_WithDifferentAITools_ReturnsFalse()
    {
        RequiredChatToolMode mode1 = new(AIFunctionFactory.Create(() => 42, "func1"));
        RequiredChatToolMode mode2 = new(AIFunctionFactory.Create(() => 43, "func2"));

        Assert.False(mode1.Equals(mode2));
        Assert.False(mode2.Equals(mode1));
    }

    [Fact]
    public void RequiredChatToolMode_Equals_WithMatchingFunctionNameAndTool_ReturnsTrue()
    {
        RequiredChatToolMode modeWithTool = new(AIFunctionFactory.Create(() => { }, "func"));
        RequiredChatToolMode modeWithFunctionName = new("func");

        Assert.True(modeWithTool.Equals(modeWithFunctionName));
        Assert.True(modeWithFunctionName.Equals(modeWithTool));
    }

    [Fact]
    public void RequiredChatToolMode_Equals_WithNonMatchingFunctionNameAndTool_ReturnsFalse()
    {
        RequiredChatToolMode modeWithTool = new(AIFunctionFactory.Create(() => { }, "func1"));
        RequiredChatToolMode modeWithFunctionName = new("func2");

        Assert.False(modeWithTool.Equals(modeWithFunctionName));
        Assert.False(modeWithFunctionName.Equals(modeWithTool));
    }

    [Fact]
    public void RequiredChatToolMode_Equals_WithBothNull_ReturnsTrue()
    {
        RequiredChatToolMode mode1 = new((AITool?)null);
        RequiredChatToolMode mode2 = new((string?)null);

        Assert.True(mode1.Equals(mode2));
        Assert.True(mode2.Equals(mode1));
        Assert.Equal(mode1.GetHashCode(), mode2.GetHashCode());
    }

    [Fact]
    public void RequiredChatToolMode_Equals_WithNullAndSpecific_ReturnsFalse()
    {
        RequiredChatToolMode modeWithTool = new(AIFunctionFactory.Create(() => { }, "func"));
        RequiredChatToolMode modeWithNull = new((AITool?)null);

        Assert.False(modeWithTool.Equals(modeWithNull));
        Assert.False(modeWithNull.Equals(modeWithTool));
    }

    [Fact]
    public void RequiredChatToolMode_GetHashCode_ConsistentForSameInstance()
    {
        RequiredChatToolMode mode = new(AIFunctionFactory.Create(() => { }, "func"));
        Assert.Equal(mode.GetHashCode(), mode.GetHashCode());
    }

    [Fact]
    public void RequiredChatToolMode_GetHashCode_WithNullTool_ReturnsTypeHashCode()
    {
        Assert.Equal(typeof(RequiredChatToolMode).GetHashCode(), new RequiredChatToolMode((AITool?)null).GetHashCode());
    }

    [Fact]
    public void RequiredChatToolMode_GetHashCode_WithFunctionName_ReturnsStringHashCode()
    {
        Assert.Equal("testFunc".GetHashCode(), new RequiredChatToolMode("testFunc").GetHashCode());
    }

    [Fact]
    public void RequiredChatToolMode_GetHashCode_WithTool_ReturnsToolHashCode()
    {
        RequiredChatToolMode mode = new(AIFunctionFactory.Create(() => { }, "func"));
        Assert.Equal("func".GetHashCode(), mode.GetHashCode());
    }

    [Fact]
    public void RequiredChatToolMode_RequiredTool_IsNotSerialized()
    {
        RequiredChatToolMode mode = new(AIFunctionFactory.Create(() => { }, "func"));
        Assert.Equal(
            """{"$type":"required","requiredFunctionName":"func"}""",
            JsonSerializer.Serialize(mode, TestJsonSerializerContext.Default.ChatToolMode));
    }

    [Fact]
    public void RequiredChatToolMode_DeserializationDoesNotRestoreRequiredTool()
    {
        RequiredChatToolMode originalMode = new(AIFunctionFactory.Create(() => { }, "func"));

        var deserializedMode = JsonSerializer.Deserialize(
            JsonSerializer.Serialize(originalMode, TestJsonSerializerContext.Default.ChatToolMode),
            TestJsonSerializerContext.Default.ChatToolMode) as RequiredChatToolMode;

        Assert.NotNull(deserializedMode);
        Assert.Equal("func", deserializedMode.RequiredFunctionName);
        Assert.Null(deserializedMode.RequiredTool);
    }

    [Fact]
    public void RequiredChatToolMode_Equals_HandlesMixedToolAndFunctionNameScenarios()
    {
        RequiredChatToolMode modeWithTool1 = new(AIFunctionFactory.Create(() => 42, "sameName"));
        RequiredChatToolMode modeWithTool2 = new(AIFunctionFactory.Create(() => 43, "sameName"));
        RequiredChatToolMode modeWithFunctionName = new("sameName");

        Assert.True(modeWithTool1.Equals(modeWithTool2));

        Assert.True(modeWithTool1.Equals(modeWithFunctionName));
        Assert.True(modeWithTool2.Equals(modeWithFunctionName));
        Assert.True(modeWithFunctionName.Equals(modeWithTool1));
        Assert.True(modeWithFunctionName.Equals(modeWithTool2));
    }

    [Fact]
    public void RequiredChatToolMode_Equals_WithNonFunctionTools()
    {
        TestNonFunctionTool tool1 = new("tool1");
        RequiredChatToolMode mode1 = new(tool1);
        RequiredChatToolMode mode2 = new(new TestNonFunctionTool("tool2"));
        RequiredChatToolMode mode3 = new(tool1);

        Assert.True(mode1.Equals(mode3));
        Assert.False(mode1.Equals(mode2));
        Assert.Equal(mode1.GetHashCode(), mode3.GetHashCode());
    }

    private sealed class TestNonFunctionTool(string name) : AITool
    {
        public override string Name => name;
        public override string Description => "Non-function tool";
    }
}
