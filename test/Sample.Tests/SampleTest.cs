// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        }

        [Theory]
        [InlineData(1, "Hi")]
        [InlineData(2, "Hi")]
        [InlineData(3, "Hi")]
        public void TheoryTest2(int x, string s)
        {
        }
    }
}