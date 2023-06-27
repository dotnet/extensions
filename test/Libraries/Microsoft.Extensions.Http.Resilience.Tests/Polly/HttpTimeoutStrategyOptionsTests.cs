// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Polly;

public class HttpTimeoutStrategyOptionsTests
{
#pragma warning disable S2330
    private readonly HttpTimeoutStrategyOptions _testObject;

    public HttpTimeoutStrategyOptionsTests()
    {
        _testObject = new HttpTimeoutStrategyOptions();
    }

    [Fact]
    public void Ctor_Defaults()
    {
        _testObject.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        _testObject.OnTimeout.Should().BeNull();
    }
}
