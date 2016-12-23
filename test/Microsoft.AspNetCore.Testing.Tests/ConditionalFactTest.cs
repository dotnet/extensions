// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class ConditionalFactTest
    {
        [ConditionalFact(Skip = "Test is always skipped.")]
        public void ConditionalFactSkip()
        {
            Assert.True(false, "This test should always be skipped.");
        }
    }
}