// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Gen.Logging.Parsing;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class TemplatesExtractorTests
{
    [Theory]
    [InlineData("c", 1)]
    [InlineData("test", 4)]
    [InlineData("toast", 2)]
    [InlineData("October", 4)]
    [InlineData("AaBbZz", 1)]
    [InlineData("NewLine \n Test", 8)]
    public void Should_FindIndexOfAny_Correctly(string message, int expectedResult)
    {
        var result = TemplateProcessor.FindIndexOfAny(message, new[] { '\n', 'a', 'b', 'z' }, 0, message.Length);
        Assert.Equal(expectedResult, result);
    }
}
