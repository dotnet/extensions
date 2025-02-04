// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;
public class SamplingParametersTests
{
    [Fact]
    public void EqualsOperators()
    {
        var testInstance = new SamplingParameters(LogLevel.Trace, nameof(SamplingParameters), 0);
        var testInstance2 = new SamplingParameters(LogLevel.Trace, nameof(SamplingParameters), 0);
        testInstance.Equals(testInstance2).Should().BeTrue();
        testInstance.GetHashCode().Should().Be(testInstance2.GetHashCode());
        testInstance.Equals(new object()).Should().BeFalse();
        testInstance.Equals((object)testInstance2).Should().BeTrue();
        testInstance.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void EqualAndNotEqualOperators()
    {
        var testInstance = new SamplingParameters(LogLevel.Trace, nameof(SamplingParameters), 0);
        var testInstance2 = new SamplingParameters(LogLevel.Trace, nameof(SamplingParameters), 0);
        (testInstance == testInstance2).Should().BeTrue();
        (testInstance != testInstance2).Should().BeFalse();
    }
}
