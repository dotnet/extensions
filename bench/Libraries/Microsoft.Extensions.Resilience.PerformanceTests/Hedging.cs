// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Extensions.Resilience.Polly.Bench.Internals;
using Polly;

namespace Microsoft.Extensions.Resilience.Polly.Bench;

public class Hedging
{
    private const int HedgingAttempts = 2;
    private readonly TimeSpan _hedgingDelay = TimeSpan.FromMilliseconds(3);
    private readonly CancellationToken _cancellationToken = new CancellationTokenSource().Token;
    private IAsyncPolicy<Result> _hedgingPolicy = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var factory = HedgingUtilities.CreatePolicyFactory();

        _hedgingPolicy = factory.CreateHedgingPolicy(
            "hedging-policy",
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            (HedgingTaskProviderArguments _, [NotNullWhen(true)] out Task<Result>? result) =>
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            {
                result = HedgingUtilities.SuccessTask;
                return true;
            },
            new HedgingPolicyOptions<Result>
            {
                HedgingDelay = _hedgingDelay,
                MaxHedgedAttempts = HedgingAttempts,
                ShouldHandleException = (e) => e is InvalidOperationException,
                ShouldHandleResultAsError = r => r == Result.TransientError
            });
    }

    [Benchmark(Baseline = true)]
    public Task HedgingManual_Success()
    {
        return HedgingUtilities.ExecuteWithManualHedging(_successFunc, _successFunc, _hedgingDelay, _cancellationToken);
    }

    [Benchmark]
    public Task Hedging_Success()
    {
        return _hedgingPolicy.ExecuteAsync(_successFunc, _cancellationToken);
    }

    [Benchmark]
    public Task HedgingManual_SuccessOnSecondaryTry()
    {
        return HedgingUtilities.ExecuteWithManualHedging(_errorFunc, _successFunc, _hedgingDelay, _cancellationToken);
    }

    [Benchmark]
    public Task HedgingManual_RealAwaiting_SuccessOnSecondaryTry()
    {
        return HedgingUtilities.ExecuteWithManualHedging(_errorFunc, _longSuccessFunc, _hedgingDelay, _cancellationToken);
    }

    [Benchmark]
    public Task Hedging_SuccessOnSecondaryTry()
    {
        return _hedgingPolicy.ExecuteAsync(_errorFunc, _cancellationToken);
    }

    [Benchmark]
    public Task HedgingManual_FirstTryThrows_SuccessOnSecondaryTry()
    {
        return HedgingUtilities.ExecuteWithManualHedging(_exceptionFunc, _successFunc, _hedgingDelay, _cancellationToken);
    }

    [Benchmark]
    public Task Hedging_FirstTryThrows_SuccessOnSecondaryTry()
    {
        return _hedgingPolicy.ExecuteAsync(_exceptionFunc, _cancellationToken);
    }

    private static readonly Func<CancellationToken, Task<Result>> _successFunc = SuccessFunc;
    private static readonly Func<CancellationToken, Task<Result>> _longSuccessFunc = LongSuccessFunc;
    private static readonly Func<CancellationToken, Task<Result>> _errorFunc = ErrorFunc;
    private static readonly Func<CancellationToken, Task<Result>> _exceptionFunc = ExceptionFunc;

    private static Task<Result> SuccessFunc(CancellationToken cancellationToken) => HedgingUtilities.SuccessTask;

    private static async Task<Result> LongSuccessFunc(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);

        return default;
    }

    private static Task<Result> ErrorFunc(CancellationToken cancellationToken) => HedgingUtilities.TransientErrorTask;

    private static Task<Result> ExceptionFunc(CancellationToken cancellationToken) => HedgingUtilities.TransientExceptionTask;
}
