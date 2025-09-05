// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class UserInputResponseContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>("id", () => new TestUserInputResponseContent(null!));
        Assert.Throws<ArgumentException>("id", () => new TestUserInputResponseContent(""));
        Assert.Throws<ArgumentException>("id", () => new TestUserInputResponseContent("\r\t\n "));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void Constructor_Roundtrips(string id)
    {
        TestUserInputResponseContent content = new(id);

        Assert.Equal(id, content.Id);
    }

    private class TestUserInputResponseContent : UserInputResponseContent
    {
        public TestUserInputResponseContent(string id)
            : base(id)
        {
        }
    }
}
