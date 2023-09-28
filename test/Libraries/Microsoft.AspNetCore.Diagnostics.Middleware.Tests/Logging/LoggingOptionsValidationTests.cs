﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Diagnostics;
using Xunit;

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
}
#endif
