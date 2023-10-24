// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Resilience.PerformanceTests;

internal sealed class NoRemoteCallHandler : DelegatingHandler
{
    private readonly Task<HttpResponseMessage> _completedResponse;

    public NoRemoteCallHandler()
    {
        _completedResponse = Task.FromResult(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        return _completedResponse;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
    }
}
