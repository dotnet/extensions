// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Http.Logging;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Test.Logging.Internal;

internal sealed class TestLoggingHandler : DelegatingHandler
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Base class disposes the handler")]
    public TestLoggingHandler(IHttpClientLogger logger, HttpMessageHandler? innerHandler)
        : base(CreateInternalBclLoggingHandler(logger, innerHandler))
    {
    }

    private static DelegatingHandler CreateInternalBclLoggingHandler(IHttpClientLogger logger, HttpMessageHandler? innerHandler)
    {
        var handlerType = typeof(IHttpClientFactory).Assembly.GetType("Microsoft.Extensions.Http.Logging.HttpClientLoggerHandler");
        Assert.NotNull(handlerType);
        var handler = Activator.CreateInstance(handlerType, logger);
        Assert.NotNull(handler);

        var delegatingHandler = Assert.IsAssignableFrom<DelegatingHandler>(handler);
        if (innerHandler is not null)
        {
            delegatingHandler.InnerHandler = innerHandler;
        }
        return delegatingHandler;
    }
}
