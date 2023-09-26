// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;

internal sealed class FaultInjectionContextMessageHandler : DelegatingHandler
{
    private readonly string _chaosPolicyOptionsGroupName;

    public FaultInjectionContextMessageHandler(string chaosPolicyOptionsGroupName)
    {
        _chaosPolicyOptionsGroupName = chaosPolicyOptionsGroupName;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = request.GetPolicyExecutionContext();

        if (context == null)
        {
            context = [];
            request.SetPolicyExecutionContext(context);
        }

        _ = context
            .WithCallingRequestMessage(request)
            .WithFaultInjection(_chaosPolicyOptionsGroupName);

        return base.SendAsync(request, cancellationToken);
    }
}
