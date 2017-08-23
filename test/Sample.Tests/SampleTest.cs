// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Sample.Tests
{
    public class SampleTest
    {
        [Fact]
        public void True_is_true()
        {
            Assert.True(true);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TheoryTest1(int x)
        {
            Assert.InRange(x, 1, 3);
        }

        [Theory]
        [InlineData(1, "Hi")]
        [InlineData(2, "Hi")]
        [InlineData(3, "Hi")]
        public void TheoryTest2(int x, string s)
        {
            Assert.InRange(x, 1, 3);
            Assert.Equal("Hi", s);
        }

        [Fact]
        public async Task SampleAsyncTest()
        {
            await Task.Delay(01);
        }
    }
}