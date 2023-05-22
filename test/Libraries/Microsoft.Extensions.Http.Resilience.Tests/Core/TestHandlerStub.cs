// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Resilience.Test;

public class TestHandlerStub : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public TestHandlerStub(HttpStatusCode responseStatus)
#pragma warning disable CA2000 // Dispose objects before losing scope
    : this(new HttpResponseMessage(responseStatus))
#pragma warning restore CA2000 // Dispose objects before losing scope
    {
    }

    public TestHandlerStub(HttpResponseMessage responseMessage)
        : this((_, _) => Task.FromResult(responseMessage))
    {
    }

    public TestHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}
