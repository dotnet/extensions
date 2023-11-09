// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;

public static class LoggerEnrichmentOptionsTests
{
    [Fact]
    public static void Basic()
    {
        const int TestValue = 4000;

        var o = new LoggerEnrichmentOptions();
        Assert.Equal(4096, o.MaxStackTraceLength);
        Assert.False(o.CaptureStackTraces);
        Assert.False(o.UseFileInfoForStackTraces);

        o.MaxStackTraceLength = TestValue;
        o.CaptureStackTraces = true;
        o.UseFileInfoForStackTraces = true;
        Assert.Equal(TestValue, o.MaxStackTraceLength);
        Assert.True(o.CaptureStackTraces);
        Assert.True(o.UseFileInfoForStackTraces);
    }

    [Fact]
    public static void Validation()
    {
        var o = new LoggerEnrichmentOptions();
        var v = new LoggerEnrichmentOptionsValidator();

        o.MaxStackTraceLength = 2048;
        Assert.Equal(ValidateOptionsResult.Success, v.Validate(null, o));

        o.MaxStackTraceLength = 32768;
        Assert.Equal(ValidateOptionsResult.Success, v.Validate(null, o));

        o.MaxStackTraceLength = 2047;
        Assert.NotEqual(ValidateOptionsResult.Success, v.Validate(null, o));

        o.MaxStackTraceLength = 32769;
        Assert.NotEqual(ValidateOptionsResult.Success, v.Validate(null, o));
    }
}
