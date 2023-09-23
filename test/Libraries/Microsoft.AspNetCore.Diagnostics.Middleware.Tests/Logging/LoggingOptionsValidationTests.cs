// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Options;
using Xunit;
using TOpt = System.Action<Microsoft.AspNetCore.Diagnostics.Logging.LoggingOptions>;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public class LoggingOptionsValidationTests
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
            .AddHttpLogging(x => x.RequestPathLoggingMode = (IncomingPathLoggingMode)mode)
            .BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.GetRequiredService<HttpLoggingMiddleware>());

        Assert.Equal($"Unsupported value '{mode}' for enum type 'IncomingPathLoggingMode'", ex.Message);
    }

    [Theory]
    [CombinatorialData]
    public void Should_NotThrow_OnValidPathLoggingMode(IncomingPathLoggingMode mode)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddFakeRedaction()
            .AddHttpLogging(x => x.RequestPathLoggingMode = mode)
            .BuildServiceProvider();

        var ex = Record.Exception(
            () => services.GetRequiredService<HttpLoggingMiddleware>());

        Assert.Null(ex);
    }

    [Theory]
    [CombinatorialData]
    public void Should_NotThrow_OnValidPathParameterRedactionMode(HttpRouteParameterRedactionMode mode)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddFakeRedaction()
            .AddHttpLogging(x => x.RequestPathParameterRedactionMode = mode)
            .BuildServiceProvider();

        var ex = Record.Exception(
            () => services.GetRequiredService<HttpLoggingMiddleware>());

        Assert.Null(ex);
    }

    [Theory]
    [MemberData(nameof(OptionsConfigureActions))]
    public void Should_Throw_OnInvalidOption(string fieldName, TOpt configure)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddFakeRedaction()
            .AddHttpLogging(configure)
            .BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(
            () => services.GetRequiredService<HttpLoggingMiddleware>());

        Assert.Contains($"{nameof(LoggingOptions)}.{fieldName}", ex.Message);
    }

    public static IEnumerable<object[]> OptionsConfigureActions =>
        new List<object[]>
        {
            new object[] { "BodySizeLimit", (TOpt) (x => x.BodySizeLimit = 0) },
            new object[] { "BodySizeLimit", (TOpt) (x => x.BodySizeLimit = -1) },
            new object[] { "BodySizeLimit", (TOpt) (x => x.BodySizeLimit = 1_572_865) },
            new object[] { "BodySizeLimit", (TOpt) (x => x.BodySizeLimit = int.MaxValue) },
            new object[] { "BodySizeLimit", (TOpt) (x => x.BodySizeLimit = int.MinValue) },
            new object[] { "RequestBodyReadTimeout", (TOpt) (x => x.RequestBodyReadTimeout = TimeSpan.Zero) },
            new object[] { "RequestBodyReadTimeout", (TOpt) (x => x.RequestBodyReadTimeout = TimeSpan.FromMilliseconds(-1)) },
            new object[] { "RequestBodyReadTimeout", (TOpt) (x => x.RequestBodyReadTimeout = TimeSpan.FromMilliseconds(60_001)) },
            new object[] { "RequestBodyReadTimeout", (TOpt) (x => x.RequestBodyReadTimeout = TimeSpan.FromMinutes(2)) },
            new object[] { "RequestBodyReadTimeout", (TOpt) (x => x.RequestBodyReadTimeout = TimeSpan.MaxValue) },
            new object[] { "RequestBodyReadTimeout", (TOpt) (x => x.RequestBodyReadTimeout = TimeSpan.MinValue) },
            new object[] { "RequestBodyContentTypes", (TOpt) (x => x.RequestBodyContentTypes = null!) },
            new object[] { "RequestHeadersDataClasses", (TOpt) (x => x.RequestHeadersDataClasses = null!) },
            new object[] { "ResponseBodyContentTypes", (TOpt) (x => x.ResponseBodyContentTypes = null!) },
            new object[] { "ResponseHeadersDataClasses", (TOpt) (x => x.ResponseHeadersDataClasses = null!) },
            new object[] { "RouteParameterDataClasses", (TOpt) (x => x.RouteParameterDataClasses = null!) },
            new object[] { "ExcludePathStartsWith", (TOpt) ( x=> x.ExcludePathStartsWith = null!) }
        };
}
