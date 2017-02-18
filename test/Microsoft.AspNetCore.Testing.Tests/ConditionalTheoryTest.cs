// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

        [ConditionalTheory(Skip = "Skip this")]
        [MemberData(nameof(GetInts))]
        public void ConditionalTheoriesWithSkippedMemberData(int arg)
        {
        }

        private static int _conditionalMemberDataRuns = 0;

        [ConditionalTheory]
        [InlineData(4)]
        [MemberData(nameof(GetInts))]
        public void ConditionalTheoriesWithMemberData(int arg)
        {
            _conditionalMemberDataRuns++;
            Assert.True(_conditionalTheoryRuns <= 3, $"Theory should run 2 times, but ran {_conditionalMemberDataRuns} times.");
        }

        public static TheoryData<int> GetInts
            => new TheoryData<int> { 0, 1 };

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [MemberData(nameof(GetActionTestData))]
        public void ConditionalTheoryWithFuncs(Func<int, int> func)
        {
        }

        public static TheoryData<Func<int, int>> GetActionTestData
            => new TheoryData<Func<int, int>>
            {
                (i) => i * 1
            };
    }
}