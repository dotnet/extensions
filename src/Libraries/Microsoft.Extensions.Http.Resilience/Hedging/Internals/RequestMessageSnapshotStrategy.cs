// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// This strategy creates a snapshot of <see cref="HttpRequestMessage"/> before executing the hedging to prevent race conditions when cloning and modifying the message at the same time.
/// This way, all hedged requests will have an unique instance of the message available from snapshot without the need to access the original one for cloning.
/// </summary>
internal sealed class RequestMessageSnapshotStrategy : ResilienceStrategy
{
    protected override async ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
        Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
        ResilienceContext context,
        TState state)
    {
        if (!context.Properties.TryGetValue(ResilienceKeys.RequestMessage, out var request) || request is null)
        {
            Throw.InvalidOperationException("The HTTP request message was not found in the resilience context.");
        }

        try
        {
            using var snapshot = await RequestMessageSnapshot.CreateAsync(request).ConfigureAwait(context.ContinueOnCapturedContext);
            context.Properties.Set(ResilienceKeys.RequestSnapshot, snapshot);
            return await callback(context, state).ConfigureAwait(context.ContinueOnCapturedContext);
        }
        catch (IOException e)
        {
            return Outcome.FromException<TResult>(e);
        }
    }
}
