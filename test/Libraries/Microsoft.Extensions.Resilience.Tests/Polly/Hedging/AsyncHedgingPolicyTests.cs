// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Hedging;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Extensions.Resilience.Polly.Test.Helpers;
using Microsoft.Extensions.Time.Testing;
using Polly;
using Polly.Timeout;
using Polly.Utilities;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Hedging;

[Collection(nameof(ResiliencePollyFakeClockTestsCollection))]
public sealed class AsyncHedgingPolicyTests : IDisposable
{
    private static readonly TimeSpan _longTimeout = TimeSpan.FromDays(31);
    private static readonly TimeSpan _assertTimeout = TimeSpan.FromSeconds(30);

    private static readonly List<Func<Context, CancellationToken, Task<HttpResponseMessage>>> _exceptionTasks =
        new()
        {
            (_, _) => GetExceptionAfterDelayAsync(new InvalidCastException(), 10),
            (_, _) => GetExceptionAfterDelayAsync(new InvalidOperationException(), 5),
            (_, _) => GetExceptionAfterDelayAsync(new ArgumentException(), 1)
        };

    private readonly Context _context;
    private readonly CancellationTokenSource _cts;
    private readonly FakeTimeProvider _timeProvider;

    public AsyncHedgingPolicyTests()
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
        var policyBuilder = Policy.HandleResult<string>(_ => false);
        var hedgingPolicy = new AsyncHedgingPolicy<string>(
            policyBuilder,
            HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProvider,
            HedgingTestUtilities<string>.HedgedTasksHandler.Functions.Count + 1,
            HedgingTestUtilities<string>.DefaultHedgingDelayGenerator,
            HedgingTestUtilities<string>.EmptyOnHedgingTask);

        Assert.NotNull(hedgingPolicy);
    }

    [Fact]
    public void ExecuteAsync_ZeroHedgingDelay_EnsureAllTasksSpawnedAtOnce()
    {
        int executions = 0;
        using var allExecutionsReached = new ManualResetEvent(false);
        var hedgingPolicy = new AsyncHedgingPolicy<string>(
            Policy.HandleResult<string>(result => false),
            HedgingTestUtilities<string>.HedgedTasksHandler.GetCustomTaskProvider(Execute),
            3,
            _ => TimeSpan.Zero,
            HedgingTestUtilities<string>.EmptyOnHedgingTask);

        _ = hedgingPolicy.ExecuteAsync(Execute, _cts.Token);

        Assert.True(allExecutionsReached.WaitOne(_assertTimeout));

        async Task<string> Execute(CancellationToken token)
        {
            if (Interlocked.Increment(ref executions) == 3)
            {
                allExecutionsReached.Set();
            }

            await _timeProvider.Delay(_longTimeout, token);
            return "dummy";
        }
    }

    [Fact]
    public void ExecuteAsync_InfiniteHedgingDelay_EnsureNoConcurrentExecutions()
    {
        bool executing = false;
        int executions = 0;
        using var allExecutions = new ManualResetEvent(true);
        var hedgingPolicy = new AsyncHedgingPolicy<string>(
            Policy.HandleResult<string>(result => true),
            HedgingTestUtilities<string>.HedgedTasksHandler.GetCustomTaskProvider(Execute),
            3,
            _ => TimeSpan.FromMilliseconds(-1),
            HedgingTestUtilities<string>.EmptyOnHedgingTask);

        var pending = hedgingPolicy.ExecuteAsync(Execute, _cts.Token);

        Assert.True(allExecutions.WaitOne(_assertTimeout));

        async Task<string> Execute(CancellationToken token)
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

            return "dummy";
        }
    }

    [Fact]
    public async Task ExecuteAsync_InfiniteHedgingDelay_EnsureSecondRetrySuccesfull()
    {
        var hedgingPolicy = new AsyncHedgingPolicy<string>(
            Policy.HandleResult<string>(result => true).Or<TimeoutRejectedException>(),
            HedgingTestUtilities<string>.HedgedTasksHandler.GetCustomTaskProvider(Execute),
            2,
            _ => TimeSpan.FromMilliseconds(-1),
            HedgingTestUtilities<string>.EmptyOnHedgingTask);

        var task = hedgingPolicy.ExecuteAsync(
           async (token) =>
           {
               await _timeProvider.Delay(TimeSpan.FromSeconds(10), token);
               throw new TimeoutRejectedException();
           },
           _cts.Token);

        // let the task do some work first
        Assert.False(task.Wait(10));

        // advance the time
        _timeProvider.Advance(TimeSpan.FromSeconds(15));

        Assert.Equal("success", await task);

        static Task<string> Execute(CancellationToken token) => Task.FromResult("success");
    }

    [Fact]
    public async Task ExecuteAsync_PrimaryTaskIsFailure_ShouldReturnDifferentNextOne()
    {
        var policyBuilder = Policy.HandleResult<string>(result => result == HedgingTestUtilities<string>.PrimaryStringTasks.FastTaskResult);

        var hedgingPolicy = new AsyncHedgingPolicy<string>(
           policyBuilder,
           HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProvider,
           HedgingTestUtilities<string>.HedgedTasksHandler.Functions.Count + 1,
           HedgingTestUtilities<string>.DefaultHedgingDelayGenerator,
           HedgingTestUtilities<string>.EmptyOnHedgingTask);

        var result = await hedgingPolicy.ExecuteAsync(() => HedgingTestUtilities<string>.PrimaryStringTasks.FastTask(_cts.Token));

        Assert.NotNull(result);
        Assert.NotEqual(HedgingTestUtilities<string>.PrimaryStringTasks.FastTaskResult, result);

        Assert.Contains(result, new[] { "Oranges", "Apples" });
    }

    [Fact]
    public async Task ExecuteAsync_ProviderReturnsNullTaskWhenPreviousTasksNotCompleted_ShouldReturn()
    {
        var policyBuilder = Policy.HandleResult<string>(result => false);

        var hedgingPolicy = new AsyncHedgingPolicy<string>(
           policyBuilder,
           (HedgingTaskProviderArguments htpa, out Task<string>? result) =>
           {
               if (htpa.AttemptNumber != 1)
               {
                   return HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProvider(htpa, out result);
               }

               result = null;
               return false;
           },
           HedgingTestUtilities<string>.HedgedTasksHandler.Functions.Count + 1,
           HedgingTestUtilities<string>.DefaultHedgingDelayGenerator,
           HedgingTestUtilities<string>.EmptyOnHedgingTask);

        var result = await hedgingPolicy.ExecuteAsync(
            () =>
            HedgingTestUtilities<string>.PrimaryStringTasks.SlowTask(_context, _cts.Token));

        Assert.NotNull(result);
        Assert.Equal("I am so slow!", result);
    }

    [Fact]
    public async Task ExecuteAsync_ProviderReturnsNullTaskWhenPreviousTaskAlreadyCompleted_ShouldNotThrow()
    {
        var policyBuilder = Policy.HandleResult<string>(result => false);

        var hedgingPolicy = new AsyncHedgingPolicy<string>(
           policyBuilder,
           (HedgingTaskProviderArguments htpa, out Task<string>? result) =>
           {
               if (htpa.AttemptNumber != 4)
               {
                   return HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProvider(htpa, out result);
               }

               result = null;
               return false;
           },
           HedgingTestUtilities<string>.HedgedTasksHandler.Functions.Count + 1,
           HedgingTestUtilities<string>.DefaultHedgingDelayGenerator,
           HedgingTestUtilities<string>.EmptyOnHedgingTask);

        var result = await hedgingPolicy.ExecuteAsync(
            () =>
            HedgingTestUtilities<string>.PrimaryStringTasks.InstantTask());

        Assert.NotNull(result);
        Assert.Equal(HedgingTestUtilities<string>.PrimaryStringTasks.InstantTaskResult, result);
    }

    [Fact]
    public async Task ExecuteAsync_AllExceptionsHandledWhenTaskThrows_ShouldThrowAnyException()
    {
        var policyBuilder = Policy
            .HandleResult<HttpResponseMessage>(_ => false)
            .Or<InvalidCastException>()
            .Or<ArgumentException>()
            .Or<InvalidOperationException>()
            .Or<BadImageFormatException>();

        var hedgingPolicy = GetHedgingPolicyWithExceptions(policyBuilder);
        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => hedgingPolicy.ExecuteAsync(
                () => GetExceptionAfterDelayAsync(new BadImageFormatException(), 20)).AdvanceTimeUntilFinished(_timeProvider));

        Assert.True(ex is InvalidCastException ||
                    ex is ArgumentException ||
                    ex is InvalidOperationException ||
                    ex is BadImageFormatException,
                    "The exception must be of type one of the thrown");
    }

    [Fact]
    public async Task ExecuteAsync_AllExceptionsHandledWhenProviderThrows_ShouldThrowLastException()
    {
        var policyBuilder = Policy
            .HandleResult<HttpResponseMessage>(_ => false)
            .Or<ArgumentException>()
            .Or<BadImageFormatException>();

        var exceptionTasks = new List<Func<Context, CancellationToken, Task<HttpResponseMessage>>>
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
            .HandleResult<HttpResponseMessage>(_ => false)
            .Or<BadImageFormatException>()
            .Or<InvalidCastException>()
            .Or<ArgumentException>();

        var hedgingPolicy =
            GetHedgingPolicyWithExceptions(
                policyBuilder,
                new List<Func<Context, CancellationToken, Task<HttpResponseMessage>>>
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
        var policyBuilder = Policy
            .HandleResult<HttpResponseMessage>(_ => false)
            .Or<InvalidCastException>()
            .Or<ArgumentException>()
            .Or<InvalidOperationException>();

        var hedgingPolicy = GetHedgingPolicyWithExceptions(policyBuilder);
        await Assert.ThrowsAsync<BadImageFormatException>(
            () => hedgingPolicy.ExecuteAsync(
                () =>
                GetExceptionAfterDelayAsync(new BadImageFormatException())));
    }

    [Fact]
    public async Task ExecuteAsync_AllResultsAndExceptionssHandledButOneException_ShouldThrowUnhandledException()
    {
        using var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var policyBuilder = Policy
            .HandleResult<HttpResponseMessage>(response => true)
            .Or<InvalidCastException>()
            .Or<ArgumentException>();

        var exceptionTasks = new List<Func<Context, CancellationToken, Task<HttpResponseMessage>>>
            {
                (_, _) => throw new InvalidCastException(),
                (_, _) => throw new InvalidOperationException(),
                (_, _) => throw new ArgumentException()
            };

        var hedgingPolicy = GetHedgingPolicyWithExceptions(policyBuilder, exceptionTasks);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => hedgingPolicy.ExecuteAsync(
                async () =>
                {
                    await SystemClock.SleepAsync(TimeSpan.FromMilliseconds(100),
                                                                 CancellationToken.None);
                    return expectedResponse;
                }));
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public async Task ExecuteAsync_AllResultsHandledButNoExceptionHandled_ShouldThrowFirstException(bool delay)
    {
        using var expectedResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var policyBuilder = Policy.HandleResult<HttpResponseMessage>(response => true);

        var executeTime = delay ? (int)HedgingTestUtilities<string>.DefaultHedgingDelay.TotalSeconds * 2 : 0;

        var exceptionTasks = new List<Func<Context, CancellationToken, Task<HttpResponseMessage>>>
        {
            (_, t) => GetExceptionTaskAsync(new InvalidCastException(), TimeSpan.FromDays(1), t),
            (_, t) => GetExceptionTaskAsync(new BadImageFormatException(), TimeSpan.FromDays(3), t),
            (_, t) => GetExceptionTaskAsync(new BadImageFormatException(), TimeSpan.FromDays(3), t),
        };

        var hedgingPolicy = GetHedgingPolicyWithExceptions(policyBuilder, exceptionTasks);

        await Assert.ThrowsAsync<InvalidCastException>(() =>
            hedgingPolicy.ExecuteAsync(() => Task.FromResult(expectedResponse)).AdvanceTimeUntilFinished(_timeProvider, TimeSpan.FromHours(1), TimeSpan.FromDays(2)));

        async Task<HttpResponseMessage> GetExceptionTaskAsync(Exception ex, TimeSpan delay, CancellationToken cancellationToken)
        {
            await _timeProvider.Delay(delay, cancellationToken);

            throw ex;
        }
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_ShouldThrowOperationCancelledException()
    {
        var policyBuilder = Policy.HandleResult<string>(_ => false);
        var hedgedTaskProvider = new HedgedTaskProvider<string>((HedgingTaskProviderArguments _, out Task<string>? _) => throw new NotSupportedException());
        var hedgingPolicy = new AsyncHedgingPolicy<string>(
            policyBuilder,
            hedgedTaskProvider,
            1,
            HedgingTestUtilities<string>.DefaultHedgingDelayGenerator,
            HedgingTestUtilities<string>.EmptyOnHedgingTask);

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
        var argsList = new List<HedgingDelayArguments>();
        var hedgingPolicy = new AsyncHedgingPolicy<string>(
            Policy.HandleResult<string>(_ => true),
            HedgingTestUtilities<string>.HedgedTasksHandler.GetCustomTaskProvider(Execute),
            3,
            GetHedgingDelay,
            HedgingTestUtilities<string>.EmptyOnHedgingTask);

        _ = hedgingPolicy.ExecuteAsync(Execute, _cts.Token);

        Assert.True(attemptsReached.WaitOne(TimeSpan.FromMinutes(1)));

#pragma warning disable S4158 // Empty collections should not be accessed or iterated
        Assert.Equal(0, argsList[0].AttemptNumber);
        Assert.Equal(1, argsList[1].AttemptNumber);
#pragma warning restore S4158 // Empty collections should not be accessed or iterated

        TimeSpan GetHedgingDelay(HedgingDelayArguments args)
        {
            argsList.Add(args);
            if (Interlocked.Increment(ref calls) == 2)
            {
                attemptsReached.Set();
            }

            return TimeSpan.FromSeconds(1);
        }

        async Task<string> Execute(CancellationToken token)
        {
            await _timeProvider.Delay(TimeSpan.FromDays(1), token);
            return "dummy";
        }
    }

    [Fact(Skip = "Flaky")]
    public async Task ExecuteAsync_NegativeHedgingDelay_EnsureRespected()
    {
        var called = false;
        var hedgingPolicy = new AsyncHedgingPolicy<string>(
            Policy.HandleResult<string>(_ => true),
            HedgingTestUtilities<string>.HedgedTasksHandler.GetCustomTaskProvider(Execute),
            3,
            (_) =>
            {
                called = true;
                return TimeSpan.FromSeconds(-5);
            },
            HedgingTestUtilities<string>.EmptyOnHedgingTask);

        await hedgingPolicy.ExecuteAsync(Execute, _cts.Token).AdvanceTimeUntilFinished(_timeProvider);

        Assert.True(called);

        async Task<string> Execute(CancellationToken token)
        {
            await _timeProvider.Delay(TimeSpan.FromDays(1), token);
            return "dummy";
        }
    }

    [Fact]
    public async Task ExecuteAsync_ExceptionCaptured_EnsureStackTrace()
    {
        // arrange
        var policyBuilder = Policy.Handle<InvalidOperationException>(_ => true).OrResult<string>(v => true);
        var hedgingPolicy = new AsyncHedgingPolicy<string>(
            policyBuilder,
            NextTask,
            3,
            (_) =>
            {
                return TimeSpan.FromSeconds(-5);
            },
            HedgingTestUtilities<string>.EmptyOnHedgingTask);

        // act
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => hedgingPolicy.ExecuteAsync(PrimaryTaskThatThrowsError));

        // assert
        Assert.Equal("Forced Error", error.Message);
        Assert.Contains(nameof(PrimaryTaskThatThrowsError), error.StackTrace);

        Task<string> PrimaryTaskThatThrowsError() => throw new InvalidOperationException("Forced Error");

        bool NextTask(HedgingTaskProviderArguments arguments, out Task<string> task)
        {
            task = null!;
            return false;
        }
    }

    [Fact]
    public async Task ExecuteAsync_OnHedgingDelayThrows_EnsureFirstResultDisposed()
    {
        using var firstResult = new DisposableResult();

        var hedgingPolicy = new AsyncHedgingPolicy<DisposableResult>(
            Policy.HandleResult<DisposableResult>(_ => true),
            HedgingTestUtilities<DisposableResult>.HedgedTasksHandler.GetCustomTaskProvider(Execute),
            3,
            (_) => TimeSpan.FromSeconds(1),
            (_, _, _, _) => throw new InvalidOperationException("on hedging fail"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => hedgingPolicy.ExecuteAsync(Execute, _cts.Token));
        Assert.Equal("on hedging fail", error.Message);

        Assert.True(firstResult.IsDisposed);

        Task<DisposableResult> Execute(CancellationToken token) => Task.FromResult(firstResult);
    }

    private static async Task<HttpResponseMessage> GetExceptionAfterDelayAsync(Exception ex, int delayInSeconds = 0)
    {
        if (delayInSeconds != 0)
        {
            await SystemClock.SleepAsync(TimeSpan.FromSeconds(delayInSeconds), CancellationToken.None);
        }

        throw ex;
    }

    private static AsyncHedgingPolicy<HttpResponseMessage> GetHedgingPolicyWithExceptions(
        PolicyBuilder<HttpResponseMessage> policyBuilder,
        List<Func<Context, CancellationToken, Task<HttpResponseMessage>>>? exceptionTasks = null,
        Func<HedgingDelayArguments, TimeSpan>? generator = null)
    {
        bool HedgedTaskProvider(HedgingTaskProviderArguments args, out Task<HttpResponseMessage>? result)
        {
            result = exceptionTasks![args.AttemptNumber - 1].Invoke(args.Context, args.CancellationToken);
            return true;
        }

        exceptionTasks ??= _exceptionTasks;
        return new AsyncHedgingPolicy<HttpResponseMessage>(
            policyBuilder,
            HedgedTaskProvider,
            _exceptionTasks.Count + 1,
            generator ?? HedgingTestUtilities<HttpResponseMessage>.DefaultHedgingDelayGenerator,
            HedgingTestUtilities<HttpResponseMessage>.EmptyOnHedgingTask);
    }
}
