// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that checks the request body against an expected one
/// and sends back an expected response.
/// </summary>
public sealed class DelegatedHttpHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsyncDelegate) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => sendAsyncDelegate(request, cancellationToken);
}
