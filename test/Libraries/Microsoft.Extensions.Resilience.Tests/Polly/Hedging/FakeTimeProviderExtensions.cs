// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.Extensions.Resilience.Polly.Test.Hedging;
internal static class FakeTimeProviderExtensions
{
    public static async Task DelayAndAdvanceAsync(this FakeTimeProvider timeProvider, TimeSpan delay, CancellationToken cancellationToken)
    {
        var delayTask = timeProvider.Delay(delay, cancellationToken);

        timeProvider.Advance(HedgingTestUtilities<string>.DefaultHedgingDelay);

        await delayTask;
    }
}
