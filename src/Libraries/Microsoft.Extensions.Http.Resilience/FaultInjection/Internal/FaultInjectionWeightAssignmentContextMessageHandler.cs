// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.FaultInjection;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;

internal sealed class FaultInjectionWeightAssignmentContextMessageHandler : DelegatingHandler
{
    private readonly FaultPolicyWeightAssignmentsOptions _weightAssignmentOptions;

    public FaultInjectionWeightAssignmentContextMessageHandler(string httpClientName, IOptionsMonitor<FaultPolicyWeightAssignmentsOptions> weightAssignmentOptions)
    {
        _weightAssignmentOptions = weightAssignmentOptions.Get(httpClientName);
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
            .WithFaultInjection(_weightAssignmentOptions);

        return base.SendAsync(request, cancellationToken);
    }
}
