// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.Http.Logging.Test.Internal;

internal class TestHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
{
    private readonly IServiceCollection _services;

    public TestHttpMessageHandlerBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public override HttpMessageHandler Build() => throw new NotSupportedException("Test");

    public override string Name { get; set; } = string.Empty;
#pragma warning disable CS8764 // Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
    public override HttpMessageHandler? PrimaryHandler { get; set; }
#pragma warning restore CS8764 // Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
    public override IList<DelegatingHandler> AdditionalHandlers { get; } = new List<DelegatingHandler>();
    public override IServiceProvider Services => _services.BuildServiceProvider();
}
