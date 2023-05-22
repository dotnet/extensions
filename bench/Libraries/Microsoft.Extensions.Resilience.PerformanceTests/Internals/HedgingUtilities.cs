// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Telemetry.Metering;

namespace Microsoft.Extensions.Resilience.Polly.Bench.Internals;

internal enum Result
{
    Success,
    TransientError
}

internal static class HedgingUtilities
{
    public static readonly Task<Result> SuccessTask = Task.FromResult(Result.Success);

    public static readonly Task<Result> TransientErrorTask = Task.FromResult(Result.TransientError);

    public static readonly Task<Result> TransientExceptionTask = Task.FromException<Result>(new InvalidOperationException("Failed hedged attempt."));

    public static Resilience.Internal.IPolicyFactory CreatePolicyFactory()
    {
        var services = new ServiceCollection();
        PolicyFactoryServiceCollectionExtensions.AddPolicyFactory<Result>(services);
        services.RegisterMetering();
        services.AddLogging();
        return services.BuildServiceProvider().GetRequiredService<Resilience.Internal.IPolicyFactory>();
    }

    public static async Task<Result> ExecuteWithManualHedging(
        Func<CancellationToken, Task<Result>> executeFunc,
        Func<CancellationToken, Task<Result>> retryFunc,
        TimeSpan hedginDelay,
        CancellationToken cancellationToken)
    {
        using var delayCancellation = new CancellationTokenSource();
        var delay = Task.Delay(hedginDelay, delayCancellation.Token);
        var executeTask = executeFunc(cancellationToken);
        var finishedTask = await Task.WhenAny(delay, executeTask).ConfigureAwait(false);

        if (finishedTask == delay)
        {
            return await retryFunc(cancellationToken).ConfigureAwait(false);
        }

        await delayCancellation.CancelAsync().ConfigureAwait(false);

        Result result = default;

        try
        {
            result = await executeTask.ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            result = Result.TransientError;
        }

        if (result == Result.TransientError)
        {
            return await retryFunc(cancellationToken).ConfigureAwait(false);
        }

        return result;
    }
}
