// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class UserInputResponseContentTests
{
    private class TestUserInputResponseContent : UserInputResponseContent
    {
        public TestUserInputResponseContent(string id)
            : base(id)
        {
        }
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void Constructor_SetsId(string id)
    {
        var content = new TestUserInputResponseContent(id);
        Assert.Equal(id, content.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsOnNullOrWhitespace(string? id)
    {
        Assert.ThrowsAny<ArgumentException>(() => new TestUserInputResponseContent(id!));
    }
}
