// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Options;
using Xunit;
using TOpt = System.Action<Microsoft.AspNetCore.Telemetry.LoggingRedactionOptions>;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test;

public class LoggingOptionsValidationTest
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(-1)]
    public void Should_Throw_OnInvalidPathLoggingMode(int mode)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddFakeRedaction()
            .AddHttpLoggingRedaction(x => x.RequestPathLoggingMode = (IncomingPathLoggingMode)mode)
            .BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.GetRequiredService<IHttpLoggingInterceptor>());

        Assert.Equal($"Unsupported value '{mode}' for enum type 'IncomingPathLoggingMode'", ex.Message);
    }

    [Theory]
    [CombinatorialData]
    public void Should_NotThrow_OnValidPathLoggingMode(IncomingPathLoggingMode mode)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddFakeRedaction()
            .AddHttpLoggingRedaction(x => x.RequestPathLoggingMode = mode)
            .BuildServiceProvider();

        var ex = Record.Exception(
            () => services.GetRequiredService<IHttpLoggingInterceptor>());

        Assert.Null(ex);
    }

    [Theory]
    [CombinatorialData]
    public void Should_NotThrow_OnValidPathParameterRedactionMode(HttpRouteParameterRedactionMode mode)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddFakeRedaction()
            .AddHttpLoggingRedaction(x => x.RequestPathParameterRedactionMode = mode)
            .BuildServiceProvider();

        var ex = Record.Exception(
            () => services.GetRequiredService<IHttpLoggingInterceptor>());

        Assert.Null(ex);
    }

    [Theory]
    [MemberData(nameof(OptionsConfigureActions))]
    public void Should_Throw_OnInvalidOption(string fieldName, TOpt configure)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddFakeRedaction()
            .AddHttpLoggingRedaction(configure)
            .BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(
            () => services.GetRequiredService<IHttpLoggingInterceptor>());

        Assert.Contains($"{nameof(LoggingRedactionOptions)}.{fieldName}", ex.Message);
    }

    public static IEnumerable<object[]> OptionsConfigureActions =>
        new List<object[]>
        {
            new object[] { "RequestHeadersDataClasses", (TOpt) (x => x.RequestHeadersDataClasses = null!) },
            new object[] { "ResponseHeadersDataClasses", (TOpt) (x => x.ResponseHeadersDataClasses = null!) },
            new object[] { "RouteParameterDataClasses", (TOpt) (x => x.RouteParameterDataClasses = null!) },
            new object[] { "ExcludePathStartsWith", (TOpt) ( x=> x.ExcludePathStartsWith = null!) }
        };
}
#endif
