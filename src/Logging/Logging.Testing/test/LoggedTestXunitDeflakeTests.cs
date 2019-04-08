// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Extensions.Logging.Testing.Tests
{
    [Deflake]
    public class LoggedTestXunitDeflakeTests : LoggedTest
    {
        public static int _runCount = 0;

        [Fact]
        [Deflake(5)]
        public void DeflakeLimitIsSetCorrectly()
        {
            Assert.Equal(5, RetryContext.Limit);
        }

        [Fact]
        [Deflake(5)]
        public void DeflakeRunsTestSpecifiedNumberOfTimes()
        {
            Assert.Equal(RetryContext.CurrentIteration, _runCount);
            _runCount++;
        }

        [Fact]
        public void DeflakeCanBeSetOnClass()
        {
            Assert.Equal(10, RetryContext.Limit);
        }
    }

    public class LoggedTestXunitDeflakeAssemblyTests : LoggedTest
    {
        [Fact]
        public void DeflakeCanBeSetOnAssembly()
        {
            Assert.Equal(1, RetryContext.Limit);
        }
    }
}
