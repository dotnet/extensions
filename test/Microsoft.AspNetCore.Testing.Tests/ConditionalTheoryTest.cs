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
    }
}