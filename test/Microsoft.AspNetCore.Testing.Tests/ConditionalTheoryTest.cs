// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class ConditionalTheoryTest
    {
        [ConditionalTheory(Skip = "Test is always skipped.")]
        [InlineData(0)]
        public void ConditionalTheorySkip(int arg)
        {
            Assert.True(false, "This test should always be skipped.");
        }

        private static int _conditionalTheoryRuns = 0;

        [ConditionalTheory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2, Skip = "Skip these data")]
        public void ConditionalTheoryRunOncePerDataLine(int arg)
        {
            _conditionalTheoryRuns++;
            Assert.True(_conditionalTheoryRuns <= 2, $"Theory should run 2 times, but ran {_conditionalTheoryRuns} times.");
        }

        [ConditionalTheory, Trait("Color", "Blue")]
        [InlineData(1)]
        public void ConditionalTheoriesShouldPreserveTraits(int arg)
        {
            Assert.True(true);
        }
    }
}