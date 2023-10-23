// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.Extensions.Http.Diagnostics;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test.Resilience;

public class FailureResultContextTests
{
    [Fact]
    public void ContextFactory_CreateTheObject()
    {
        var context = FailureResultContext.Create();

        context.FailureSource.Should().Be(TelemetryConstants.Unknown);
        context.AdditionalInformation.Should().Be(TelemetryConstants.Unknown);
        context.FailureReason.Should().Be(TelemetryConstants.Unknown);
    }
}
