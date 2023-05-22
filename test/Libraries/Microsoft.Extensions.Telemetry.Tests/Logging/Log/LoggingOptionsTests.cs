// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Telemetry.Logging.Test.Log;

public class LoggingOptionsTests
{
    private readonly LoggingOptions _sut = new();

    [Fact]
    public void CanSetAndGetIncludeScopes()
    {
        const bool TestValue = false;
        _sut.IncludeScopes = TestValue;
        Assert.Equal(TestValue, _sut.IncludeScopes);
    }

    [Fact]
    public void CanSetAndGetUseFormattedMessage()
    {
        const bool TestValue = false;
        _sut.UseFormattedMessage = TestValue;
        Assert.Equal(TestValue, _sut.UseFormattedMessage);
    }

    [Fact]
    public void CanSetAndGetStackTraceLimit()
    {
        const int TestValue = 4000;
        _sut.MaxStackTraceLength = TestValue;
        Assert.Equal(TestValue, _sut.MaxStackTraceLength);
    }
}
