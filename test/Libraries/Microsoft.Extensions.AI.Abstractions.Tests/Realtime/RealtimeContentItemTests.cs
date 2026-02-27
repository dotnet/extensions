// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class RealtimeContentItemTests
{
    [Fact]
    public void Constructor_WithContentsOnly_PropsDefaulted()
    {
        IList<AIContent> contents = [new TextContent("Hello")];
        var item = new RealtimeContentItem(contents);

        Assert.Same(contents, item.Contents);
        Assert.Null(item.Id);
        Assert.Null(item.Role);
        Assert.Null(item.RawRepresentation);
    }

    [Fact]
    public void Constructor_WithAllArgs_PropsRoundtrip()
    {
        IList<AIContent> contents = [new TextContent("Hello"), new TextContent("World")];
        var item = new RealtimeContentItem(contents, "item_123", ChatRole.User);

        Assert.Same(contents, item.Contents);
        Assert.Equal("item_123", item.Id);
        Assert.Equal(ChatRole.User, item.Role);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        IList<AIContent> contents = [new TextContent("Initial")];
        var item = new RealtimeContentItem(contents);

        IList<AIContent> newContents = [new TextContent("Updated")];
        item.Id = "new_id";
        item.Role = ChatRole.Assistant;
        item.Contents = newContents;
        item.RawRepresentation = "raw_data";

        Assert.Equal("new_id", item.Id);
        Assert.Equal(ChatRole.Assistant, item.Role);
        Assert.Same(newContents, item.Contents);
        Assert.Equal("raw_data", item.RawRepresentation);
    }

    [Fact]
    public void Constructor_WithFunctionContent_NoIdOrRole()
    {
        var functionCall = new FunctionCallContent("call_1", "myFunc");
        IList<AIContent> contents = [functionCall];
        var item = new RealtimeContentItem(contents);

        Assert.Null(item.Id);
        Assert.Null(item.Role);
        Assert.Single(item.Contents);
        Assert.IsType<FunctionCallContent>(item.Contents[0]);
    }
}
