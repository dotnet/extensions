// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// This policy creates a snapshot of <see cref="HttpRequestMessage"/> before executing the hedging to prevent race conditions when cloning and modifying the message at the same time.
/// This way, all hedged requests will have an unique instance of the message available from snapshot without the need to access the original one for cloning.
/// </summary>
internal sealed class RequestMessageSnapshotPolicy : AsyncPolicy<HttpResponseMessage>
{
    private readonly IRequestClonerInternal _requestCloner;
    private readonly Func<Context, HttpRequestMessage?> _requestProvider;
    private readonly Action<Context, IHttpRequestMessageSnapshot> _snapshotSetter;

    public RequestMessageSnapshotPolicy(string pipelineName, IRequestClonerInternal requestCloner)
    {
        _requestCloner = requestCloner;
        _requestProvider = ContextExtensions.CreateRequestMessageProvider(pipelineName);
        _snapshotSetter = HedgingContextExtensions.CreateRequestMessageSnapshotSetter(pipelineName);
    }

    protected override async Task<HttpResponseMessage> ImplementationAsync(
        Func<Context, CancellationToken, Task<HttpResponseMessage>> action,
        Context context,
        CancellationToken cancellationToken,
        bool continueOnCapturedContext)
    {
        using var snapshot = _requestCloner.CreateSnapshot(_requestProvider(context)!);
        _snapshotSetter(context, snapshot);

        return await action(context, cancellationToken).ConfigureAwait(continueOnCapturedContext);
    }
}
