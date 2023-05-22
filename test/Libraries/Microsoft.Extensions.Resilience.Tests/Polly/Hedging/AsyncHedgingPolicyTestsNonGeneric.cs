// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Hedging;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Extensions.Time.Testing;
using Polly;
using Polly.Utilities;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Hedging;

[Collection(nameof(ResiliencePollyFakeClockTestsCollection))]
public sealed class AsyncHedgingPolicyTestsNonGeneric : IDisposable
{
    private static readonly TimeSpan _longTimeout = TimeSpan.FromDays(365);
    private static readonly TimeSpan _assertTimeout = TimeSpan.FromSeconds(30);

    private static readonly List<Func<Context, CancellationToken, Task<EmptyStruct>>> _exceptionTasks =
        new()
        {
            (_, _) => GetExceptionAfterDelayAsync(new InvalidCastException(), 10),
            (_, _) => GetExceptionAfterDelayAsync(new InvalidOperationException(), 5),
            (_, _) => GetExceptionAfterDelayAsync(new ArgumentException(), 1)
        };

    private readonly Context _context;
    private readonly CancellationTokenSource _cts;
    private readonly FakeTimeProvider _timeProvider;

    public AsyncHedgingPolicyTestsNonGeneric()
    {
        _context = new Context();
        _cts = new CancellationTokenSource();
        _timeProvider = new FakeTimeProvider();
        SystemClock.SleepAsync = _timeProvider.DelayAndAdvanceAsync;
    }

    public void Dispose()
    {
        _cts.Dispose();
        SystemClock.Reset();
    }

    [Fact]
    public void Constructor_ShouldCreatePolicy()
    {
        var policyBuilder = Policy.Handle<Exception>();
        var hedgingPolicy = new AsyncHedgingPolicy(
            policyBuilder,
            HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.FunctionsProvider,
            HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.Functions.Count + 1,
            HedgingTestUtilities<EmptyStruct>.DefaultHedgingDelayGenerator,
            HedgingTestUtilities<EmptyStruct>.EmptyOnHedgingTask);

        Assert.NotNull(hedgingPolicy);
    }

    [Fact]
    public void ExecuteAsync_ZeroHedgingDelay_EnsureAllTasksSpawnedAtOnce()
    {
        int executions = 0;
        using var allExecutionsReached = new ManualResetEvent(false);
        var hedgingPolicy = new AsyncHedgingPolicy(
            Policy.Handle<Exception>(),
            HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.GetCustomTaskProvider(Execute),
            3,
            _ => TimeSpan.Zero,
            HedgingTestUtilities<EmptyStruct>.EmptyOnHedgingTask);

        _ = hedgingPolicy.ExecuteAsync(Execute, _cts.Token);

        Assert.True(allExecutionsReached.WaitOne(_assertTimeout));

        async Task<EmptyStruct> Execute(CancellationToken token)
        {
            if (Interlocked.Increment(ref executions) == 3)
            {
                allExecutionsReached.Set();
            }

            await _timeProvider.Delay(_longTimeout, token);
            return EmptyStruct.Instance;
        }
    }

    [Fact]
    public void ExecuteAsync_InfiniteHedgingDelay_EnsureNoConcurrentExecutions()
    {
        bool executing = false;
        int executions = 0;
        using var allExecutions = new ManualResetEvent(true);
        var hedgingPolicy = new AsyncHedgingPolicy(
            Policy.Handle<Exception>(),
            HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.GetCustomTaskProvider(Execute),
            3,
            _ => TimeSpan.FromMilliseconds(-1),
            HedgingTestUtilities<EmptyStruct>.EmptyOnHedgingTask);

        var pending = hedgingPolicy.ExecuteAsync(Execute, _cts.Token);

        Assert.True(allExecutions.WaitOne(_assertTimeout));

        async Task<EmptyStruct> Execute(CancellationToken token)
        {
            if (Interlocked.Increment(ref executions) == 3)
            {
                allExecutions.Set();
            }

            if (executing)
            {
                throw new InvalidOperationException("Concurrent execution detected!");
            }

            await SystemClock.SleepAsync(TimeSpan.FromHours(1), token);

            return EmptyStruct.Instance;
        }
    }

    [Fact]
    public async Task ExecuteAsync_ProviderReturnsNullTaskWhenPreviousTasksNotCompleted_ShouldReturn()
    {
        var policyBuilder = Policy.Handle<Exception>();

        var hedgingPolicy = new AsyncHedgingPolicy(
           policyBuilder,
           (HedgingTaskProviderArguments htpa, out Task<EmptyStruct>? result) =>
           {
               if (htpa.AttemptNumber != 1)
               {
                   return HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.FunctionsProvider(htpa, out result);
               }

               result = null;
               return false;
           },
           HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.Functions.Count + 1,
           HedgingTestUtilities<EmptyStruct>.DefaultHedgingDelayGenerator,
           HedgingTestUtilities<EmptyStruct>.EmptyOnHedgingTask);

        var result = await hedgingPolicy.ExecuteAsync(
            () =>
            HedgingTestUtilities<string>.PrimaryStringTasks.SlowTask(_context, _cts.Token));

        Assert.NotNull(result);
        Assert.Equal("I am so slow!", result);
    }

    [Fact]
    public async Task ExecuteAsync_ProviderReturnsNullTaskWhenPreviousTaskAlreadyCompleted_ShouldNotThrow()
    {
        var policyBuilder = Policy.Handle<Exception>();

        var hedgingPolicy = new AsyncHedgingPolicy(
           policyBuilder,
           (HedgingTaskProviderArguments htpa, out Task<EmptyStruct>? result) =>
           {
               if (htpa.AttemptNumber != 4)
               {
                   return HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.FunctionsProvider(htpa, out result);
               }

               result = null;
               return false;
           },
           HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.Functions.Count + 1,
           HedgingTestUtilities<EmptyStruct>.DefaultHedgingDelayGenerator,
           HedgingTestUtilities<EmptyStruct>.EmptyOnHedgingTask);

        var result = await hedgingPolicy.ExecuteAsync(
            () =>
            HedgingTestUtilities<string>.PrimaryStringTasks.InstantTask());

        Assert.NotNull(result);
        Assert.Equal(HedgingTestUtilities<string>.PrimaryStringTasks.InstantTaskResult, result);
    }

    [Fact]
    public async Task ExecuteAsync_AllExceptionsHandledWhenTaskThrows_ShouldThrowAnyException()
    {
        var policyBuilder = Policy.Handle<InvalidCastException>()
            .Or<InvalidCastException>()
            .Or<ArgumentException>()
            .Or<InvalidOperationException>()
            .Or<BadImageFormatException>();

        var hedgingPolicy = AsyncHedgingPolicyTestsNonGeneric.GetHedgingPolicyWithExceptions(policyBuilder);
        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => hedgingPolicy.ExecuteAsync(() =>
                GetExceptionAfterDelayAsync(new BadImageFormatException(), 20)).AdvanceTimeUntilFinished(_timeProvider));

        Assert.True(ex is InvalidCastException ||
                    ex is ArgumentException ||
                    ex is InvalidOperationException ||
                    ex is BadImageFormatException,
                    "The exception must be of type one of the thrown");
    }

    [Fact]
    public async Task ExecuteAsync_AllExceptionsHandledWhenProviderThrows_ShouldThrowLastException()
    {
        var policyBuilder = Policy.Handle<ArgumentException>()
            .Or<BadImageFormatException>();

        var exceptionTasks = new List<Func<Context, CancellationToken, Task<EmptyStruct>>>
            {
                (_, _) => throw new BadImageFormatException(),
                (_, _) => throw new BadImageFormatException(),
                (_, _) => throw new ArgumentException("Expected exception.")
            };

        var hedgingPolicy = GetHedgingPolicyWithExceptions(policyBuilder, exceptionTasks);
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => hedgingPolicy.ExecuteAsync(
                () => throw new BadImageFormatException()));
        Assert.Equal("Expected exception.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_AllExceptionsHandledButOne_ShouldThrowTheNonHandledOne()
    {
        var policyBuilder = Policy
            .Handle<BadImageFormatException>()
            .Or<InvalidCastException>()
            .Or<ArgumentException>();

        var hedgingPolicy =
            GetHedgingPolicyWithExceptions(
                policyBuilder,
                new List<Func<Context, CancellationToken, Task<EmptyStruct>>>
                {
                    (_, _)=> throw new InvalidCastException(),
                    (_, _)=> throw new ArgumentException(),
                    (_, _)=> throw new ReadOnlyException(),
                },
                generator: _ => TimeSpan.Zero);

        await Assert.ThrowsAsync<ReadOnlyException>(() => hedgingPolicy.ExecuteAsync(() => throw new BadImageFormatException()));
    }

    [Fact]
    public async Task ExecuteAsync_AllExceptionsHandledButFirst_ShouldThrowTheFirstOne()
    {
        var policyBuilder = Policy.Handle<InvalidCastException>()
            .Or<ArgumentException>()
            .Or<InvalidOperationException>();

        var hedgingPolicy = GetHedgingPolicyWithExceptions(policyBuilder);
        await Assert.ThrowsAsync<BadImageFormatException>(
            () => hedgingPolicy.ExecuteAsync(
                () =>
                GetExceptionAfterDelayAsync(new BadImageFormatException())));
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public async Task ExecuteAsync_NoExceptionHandled_ShouldThrowFirstException(bool delay)
    {
        var policyBuilder = Policy.Handle<Exception>(e => e is ReadOnlyException);

        var executeTime = delay ? (int)HedgingTestUtilities<string>.DefaultHedgingDelay.TotalSeconds * 2 : 0;

        var exceptionTasks = new List<Func<Context, CancellationToken, Task<EmptyStruct>>>
        {
            (_, t) => GetExceptionTaskAsync(new InvalidCastException(), TimeSpan.FromDays(1), t),
            (_, t) => GetExceptionTaskAsync(new BadImageFormatException(), TimeSpan.FromDays(3), t),
            (_, t) => GetExceptionTaskAsync(new BadImageFormatException(), TimeSpan.FromDays(3), t),
        };

        var hedgingPolicy = GetHedgingPolicyWithExceptions(policyBuilder, exceptionTasks);

        await Assert.ThrowsAsync<InvalidCastException>(() => hedgingPolicy
            .ExecuteAsync(() => throw new ReadOnlyException())
            .AdvanceTimeUntilFinished(_timeProvider, TimeSpan.FromHours(1), TimeSpan.FromDays(2)));

        async Task<EmptyStruct> GetExceptionTaskAsync(Exception ex, TimeSpan delay, CancellationToken cancellationToken)
        {
            await _timeProvider.Delay(delay, cancellationToken);

            throw ex;
        }
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_ShouldThrowOperationCancelledException()
    {
        var policyBuilder = Policy.Handle<Exception>();
        var hedgedTaskProvider = new HedgedTaskProvider<EmptyStruct>((HedgingTaskProviderArguments _, out Task<EmptyStruct>? _) => throw new NotSupportedException());
        var hedgingPolicy = new AsyncHedgingPolicy(
            policyBuilder,
            hedgedTaskProvider,
            1,
            HedgingTestUtilities<EmptyStruct>.DefaultHedgingDelayGenerator,
            HedgingTestUtilities<EmptyStruct>.EmptyOnHedgingTask);

        _cts.Cancel();

        var error = await Assert.ThrowsAsync<OperationCanceledException>(() => hedgingPolicy.ExecuteAsync(_ => Task.FromResult("dummy-result"), _cts.Token));

        // hedging implementation creates linked token once it starts executing
        // in this case we want to check the original canceled token was respected
        Assert.Equal(_cts.Token, error.CancellationToken);
    }

    [Fact]
    public void ExecuteAsync_EnsureHedgingDelayGeneratorRespected()
    {
        using var attemptsReached = new ManualResetEvent(false);
        var calls = 0;
        var hedgingPolicy = new AsyncHedgingPolicy(
            Policy.Handle<Exception>(),
            HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.GetCustomTaskProvider(Execute),
            3,
            GetHedgingDelay,
            HedgingTestUtilities<EmptyStruct>.EmptyOnHedgingTask);

        _ = hedgingPolicy.ExecuteAsync(Execute, _cts.Token);

        Assert.True(attemptsReached.WaitOne(TimeSpan.FromMinutes(1)));

        TimeSpan GetHedgingDelay(HedgingDelayArguments args)
        {
            if (Interlocked.Increment(ref calls) == 2)
            {
                attemptsReached.Set();
            }

            return TimeSpan.FromSeconds(1);
        }

        async Task<EmptyStruct> Execute(CancellationToken token)
        {
            await _timeProvider.Delay(TimeSpan.FromDays(1), token);
            return EmptyStruct.Instance;
        }
    }

    [Fact]
    public async Task ExecuteAsync_NegativeHedgingDelay_EnsureRespected()
    {
        var called = false;
        var hedgingPolicy = new AsyncHedgingPolicy(
            Policy.Handle<Exception>(),
            HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.GetCustomTaskProvider(Execute),
            3,
            (_) =>
            {
                called = true;
                return TimeSpan.FromSeconds(-5);
            },
            HedgingTestUtilities<EmptyStruct>.EmptyOnHedgingTask);

        await hedgingPolicy.ExecuteAsync(Execute, _cts.Token).AdvanceTimeUntilFinished(_timeProvider);

        Assert.True(called);

        async Task<EmptyStruct> Execute(CancellationToken token)
        {
            await _timeProvider.Delay(TimeSpan.FromDays(1), token);
            return EmptyStruct.Instance;
        }
    }

    private static async Task<EmptyStruct> GetExceptionAfterDelayAsync(Exception ex, int delayInSeconds = 0)
    {
        if (delayInSeconds != 0)
        {
            await SystemClock.SleepAsync(TimeSpan.FromSeconds(delayInSeconds), CancellationToken.None);
        }

        throw ex;
    }

    private static AsyncHedgingPolicy GetHedgingPolicyWithExceptions(
        PolicyBuilder policyBuilder,
        List<Func<Context, CancellationToken, Task<EmptyStruct>>>? exceptionTasks = null,
        Func<HedgingDelayArguments, TimeSpan>? generator = null)
    {
        bool HedgedTaskProvider(HedgingTaskProviderArguments args, out Task<EmptyStruct>? result)
        {
            result = exceptionTasks![args.AttemptNumber - 1].Invoke(args.Context, args.CancellationToken);
            return true;
        }

        exceptionTasks ??= _exceptionTasks;
        return new AsyncHedgingPolicy(
            policyBuilder,
            HedgedTaskProvider,
            _exceptionTasks.Count + 1,
            generator ?? HedgingTestUtilities<EmptyStruct>.DefaultHedgingDelayGenerator,
            HedgingTestUtilities<EmptyStruct>.EmptyOnHedgingTask);
    }
}
