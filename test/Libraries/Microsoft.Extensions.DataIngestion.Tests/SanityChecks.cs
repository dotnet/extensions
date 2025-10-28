// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.DataIngestion.Tests;

#pragma warning disable S2699 // Tests should include assertions
public class SanityChecks
{
    [ConditionalFact]
    public void JustSkipFact() => throw new SkipTestException("Facts can be disabled.");

    [ConditionalTheory]
    [InlineData("Theories can be disabled.")]
    public void JustSkipTheory(string txt) => throw new SkipTestException(txt);
}
#pragma warning restore S2699 // Tests should include assertions
