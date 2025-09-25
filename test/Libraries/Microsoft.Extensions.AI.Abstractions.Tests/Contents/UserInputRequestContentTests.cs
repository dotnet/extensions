// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class UserInputRequestContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>("id", () => new TestUserInputRequestContent(null!));
        Assert.Throws<ArgumentException>("id", () => new TestUserInputRequestContent(""));
        Assert.Throws<ArgumentException>("id", () => new TestUserInputRequestContent("\r\t\n "));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void Constructor_Roundtrips(string id)
    {
        TestUserInputRequestContent content = new(id);

        Assert.Equal(id, content.Id);
    }

    private sealed class TestUserInputRequestContent : UserInputRequestContent
    {
        public TestUserInputRequestContent(string id)
            : base(id)
        {
        }
    }
}
