// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class FunctionInvocationContextTests
{
    [Fact]
    public void Constructor_PropertiesDefaultToExpectedValues()
    {
        FunctionInvocationContext ctx = new();

        Assert.NotNull(ctx.CallContent);
        Assert.NotNull(ctx.ChatMessages);
        Assert.NotNull(ctx.Function);
        Assert.Equal(0, ctx.FunctionCallIndex);
        Assert.Equal(0, ctx.FunctionCount);
        Assert.Equal(0, ctx.Iteration);
        Assert.False(ctx.Terminate);

        Assert.Empty(ctx.ChatMessages);
        Assert.True(ctx.ChatMessages.IsReadOnly);

        Assert.Equal(nameof(FunctionInvocationContext), ctx.Function.Name);
        Assert.Empty(ctx.Function.Description);
        Assert.NotNull(ctx.Function.UnderlyingMethod);
    }

    [Fact]
    public void InvalidArgs_Throws()
    {
        FunctionInvocationContext ctx = new();
        Assert.Throws<ArgumentNullException>("value", () => ctx.CallContent = null!);
        Assert.Throws<ArgumentNullException>("value", () => ctx.ChatMessages = null!);
        Assert.Throws<ArgumentNullException>("value", () => ctx.Function = null!);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        FunctionInvocationContext ctx = new();

        List<ChatMessage> chatMessages = [];
        ctx.ChatMessages = chatMessages;
        Assert.Same(chatMessages, ctx.ChatMessages);

        AIFunction function = AIFunctionFactory.Create(() => { }, nameof(Properties_Roundtrip));
        ctx.Function = function;
        Assert.Same(function, ctx.Function);

        FunctionCallContent callContent = new(string.Empty, string.Empty, new Dictionary<string, object?>());
        ctx.CallContent = callContent;
        Assert.Same(callContent, ctx.CallContent);

        ctx.Iteration = 1;
        Assert.Equal(1, ctx.Iteration);

        ctx.FunctionCallIndex = 2;
        Assert.Equal(2, ctx.FunctionCallIndex);

        ctx.FunctionCount = 3;
        Assert.Equal(3, ctx.FunctionCount);

        ctx.Terminate = true;
        Assert.True(ctx.Terminate);
    }
}
