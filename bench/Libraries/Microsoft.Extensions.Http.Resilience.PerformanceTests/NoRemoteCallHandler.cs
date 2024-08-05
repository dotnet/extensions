// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Resilience.PerformanceTests;

internal sealed class NoRemoteCallHandler : DelegatingHandler
{
    private readonly HttpResponseMessage _response;
    private readonly Task<HttpResponseMessage> _completedResponse;
    private volatile bool _disposed;

    public NoRemoteCallHandler()
    {
        _response = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK
        };

        _completedResponse = Task.FromResult(_response);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        return _completedResponse;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _response;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _response.Dispose();
        }

        base.Dispose(disposing);
    }
}
