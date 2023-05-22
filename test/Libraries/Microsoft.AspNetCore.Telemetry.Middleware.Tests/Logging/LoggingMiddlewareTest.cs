// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;
using Moq;
using Xunit;
using IOptionsFactory = Microsoft.Extensions.Options.Options;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test;

public class LoggingMiddlewareTest
{
    [Fact]
    public void HttpLoggingMiddleware_While_Having_Attached_Debugger_Has_Infinite_Timeout_For_Reading_A_Body()
    {
        var middleware = new HttpLoggingMiddleware(
            options: IOptionsFactory.Create(new LoggingOptions()),
            logger: NullLogger<HttpLoggingMiddleware>.Instance,
            httpLogEnrichers: Array.Empty<IHttpLogEnricher>(),
            httpRouteParser: new Mock<IHttpRouteParser>().Object,
            httpRouteFormatter: new Mock<IHttpRouteFormatter>().Object,
            redactorProvider: NullRedactorProvider.Instance,
            httpRouteUtility: new Mock<IIncomingHttpRouteUtility>().Object,
            debugger: DebuggerState.Attached);

        Assert.Equal(middleware.RequestBodyReadTimeout, Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void HttpLoggingMiddleware_While_Having_Attached_Detached_Has_Timeout_Set_By_Options_For_Reading_A_Body()
    {
        var options = new LoggingOptions();

        var middleware = new HttpLoggingMiddleware(
            options: IOptionsFactory.Create(options),
            logger: NullLogger<HttpLoggingMiddleware>.Instance,
            httpLogEnrichers: Array.Empty<IHttpLogEnricher>(),
            httpRouteParser: new Mock<IHttpRouteParser>().Object,
            httpRouteFormatter: new Mock<IHttpRouteFormatter>().Object,
            redactorProvider: NullRedactorProvider.Instance,
            httpRouteUtility: new Mock<IIncomingHttpRouteUtility>().Object,
            debugger: DebuggerState.Detached);

        Assert.Equal(middleware.RequestBodyReadTimeout, options.RequestBodyReadTimeout);
    }
}
