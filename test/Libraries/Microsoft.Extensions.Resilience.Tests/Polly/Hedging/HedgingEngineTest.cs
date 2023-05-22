// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Hedging;
using Microsoft.Extensions.Resilience.Polly.Test.Helpers;
using Microsoft.Extensions.Time.Testing;
using Polly;
using Polly.Utilities;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Hedging;

[Collection(nameof(ResiliencePollyFakeClockTestsCollection))]
public sealed class HedgingEngineTest : IDisposable
{
    private readonly HedgingEngineOptions<string> _options;
    private readonly CancellationTokenSource _cts;
    private readonly Context _context;
    private readonly FakeTimeProvider _timeProvider;

    public HedgingEngineTest()
    {
        _cts = new CancellationTokenSource();
        _context = new Context();
        _options = new HedgingEngineOptions<string>(
             1 + HedgingTestUtilities<string>.HedgedTasksHandler.Functions.Count,
             HedgingTestUtilities<string>.DefaultHedgingDelayGenerator,
             ExceptionPredicates.None,
             ResultPredicates<string>.None,
             HedgingTestUtilities<string>.EmptyOnHedgingTask);

        _timeProvider = new FakeTimeProvider();
        SystemClock.SleepAsync = _timeProvider.DelayAndAdvanceAsync;
    }

    public void Dispose()
    {
        _cts?.Dispose();
        SystemClock.Reset();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAnyPossibleResult()
    {
        var result = await HedgingEngine<string>.ExecuteAsync(
            HedgingTestUtilities<string>.PrimaryStringTasks.SlowTask,
            _context,
            HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProvider,
            _options,
            false,
            _cts.Token);

        Assert.NotNull(result);
        Assert.Contains(result,
            new[]
            {
                "Oranges", "Pears", "Apples",
                 HedgingTestUtilities<string>.PrimaryStringTasks.SlowTaskResult
            });
    }

    [Fact]
    public async Task ExecuteAsync_PrimaryTaskTooSlow_EnsurePrimaryTaskResultDisposed()
    {
        var slowTaskCancelled = false;
        var slowTaskDelay = TimeSpan.FromDays(1);
        using var firstResult = new DisposableResult();

        var options = new HedgingEngineOptions<DisposableResult>(
             2,
             HedgingTestUtilities<string>.DefaultHedgingDelayGenerator,
             ExceptionPredicates.None,
             ResultPredicates<DisposableResult>.None,
             (_, _, _, _) => Task.CompletedTask);

        var result = await HedgingEngine<DisposableResult>.ExecuteAsync(
            SlowTask,
            _context,
            FastTask,
            options,
            false,
            _cts.Token);

        // the next line should be instantaneous, however let's give it some buffer to finish in rare scenarios (system overload on build machine)
        Assert.True(firstResult.OnDisposed.Task.Wait(TimeSpan.FromSeconds(5)), "Timeout while waiting for the disposal of the first result.");
        Assert.True(firstResult.IsDisposed);
        Assert.True(slowTaskCancelled);

        async Task<DisposableResult> SlowTask(Context context, CancellationToken cancellationToken)
        {
            try
            {
                // occasionally, SystemClock.SleepAsync call won't throw and just returns instead
                // so we need to handle both cases
                await SystemClock.SleepAsync(slowTaskDelay, cancellationToken);

                slowTaskCancelled = cancellationToken.IsCancellationRequested;
            }
            catch (OperationCanceledException)
            {
                slowTaskCancelled = true;
            }

            return firstResult;
        }

        bool FastTask(HedgingTaskProviderArguments arguments, out Task<DisposableResult> result)
        {
            result = Task.FromResult(new DisposableResult());
            return true;
        }
    }

    [Fact]
    public async Task ExecuteAsync_PrimaryTaskTooSlow_EnsureSecondaryResultWithoutCancelledCancellationToken()
    {
        // arrange
        var originalMessage = new DummyRequestMessage();

        var options = CreateOptions<DummyRequestMessage>();

        // act
        var result = await HedgingEngine<DummyRequestMessage>.ExecuteAsync(
            (c, token) => SlowTask(originalMessage, c, token),
            _context,
            FastTask,
            options,
            false,
            _cts.Token);

        _timeProvider.Advance(TimeSpan.FromDays(2));

        // assert
        Assert.NotEqual(result, originalMessage);
        Assert.NotEqual(result.CancellationToken, originalMessage.CancellationToken);
        Assert.False(result.CancellationToken.IsCancellationRequested);
        Assert.True(originalMessage.CancellationToken.IsCancellationRequested);

        async Task<DummyRequestMessage> SlowTask(DummyRequestMessage message, Context context, CancellationToken cancellationToken)
        {
            context["request"] = message;

            message.StoreCancellationToken(cancellationToken);

            await SystemClock.SleepAsync(TimeSpan.FromDays(1), cancellationToken);

            return message;
        }

        bool FastTask(HedgingTaskProviderArguments arguments, out Task<DummyRequestMessage> task)
        {
            var message = (arguments.Context["request"] as DummyRequestMessage)!.Clone();

            message.StoreCancellationToken(arguments.CancellationToken);

            task = Task.FromResult(message);
            return true;
        }
    }

    [Fact]
    public async Task ExecuteAsync_EnsureCancellationTokenLinkingBroken()
    {
        // arrange
        var originalMessage = new DummyRequestMessage();

        var options = CreateOptions<DummyRequestMessage>();

        // act
        var result = await HedgingEngine<DummyRequestMessage>.ExecuteAsync(
            (c, token) => SlowTask(originalMessage, c, token),
            _context,
            FastTask,
            options,
            false,
            _cts.Token);

        _timeProvider.Advance(TimeSpan.FromDays(2));

        Assert.NotEqual(_cts.Token, originalMessage.CancellationToken);
        Assert.NotEqual(_cts.Token, result.CancellationToken);

        Assert.False(result.CancellationToken.IsCancellationRequested);
        _cts.Cancel();
        Assert.False(result.CancellationToken.IsCancellationRequested);

        async Task<DummyRequestMessage> SlowTask(DummyRequestMessage message, Context context, CancellationToken cancellationToken)
        {
            context["request"] = message;

            message.StoreCancellationToken(cancellationToken);

            await SystemClock.SleepAsync(TimeSpan.FromDays(1), cancellationToken);

            return message;
        }

        bool FastTask(HedgingTaskProviderArguments arguments, out Task<DummyRequestMessage> task)
        {
            var message = (arguments.Context["request"] as DummyRequestMessage)!.Clone();

            message.StoreCancellationToken(arguments.CancellationToken);

            task = Task.FromResult(message);
            return true;
        }
    }

    [Fact]
    public async Task ExecuteAsync_EnsureBackroundWorkInSuccesfullCallNotCancelled()
    {
        List<Task> backroundTasks = new List<Task>();

        var options = CreateOptions<bool>();

        // act
        var result = await HedgingEngine<bool>.ExecuteAsync(
            (_, token) => SlowTask(token),
            _context,
            FastTask,
            options,
            false,
            _cts.Token);

        _timeProvider.Advance(TimeSpan.FromDays(2));

        await Assert.ThrowsAsync<TaskCanceledException>(() => backroundTasks[0]);

        // background task is still pending
        Assert.False(backroundTasks[1].IsCompleted);

        _cts.Cancel();

        // background task is still pending
        Assert.False(backroundTasks[1].IsCompleted);

        async Task<bool> SlowTask(CancellationToken cancellationToken)
        {
            backroundTasks.Add(BackroundWork(cancellationToken));

            await SystemClock.SleepAsync(TimeSpan.FromDays(1), cancellationToken);

            return true;
        }

        bool FastTask(HedgingTaskProviderArguments arguments, out Task<bool> task)
        {
            backroundTasks.Add(BackroundWork(arguments.CancellationToken));

            task = Task.FromResult(true);
            return true;
        }

        async Task BackroundWork(CancellationToken cancellationToken) => await Task.Delay(TimeSpan.FromDays(24), cancellationToken);
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void CancellationPair_Create_Ok(bool canBeCancelled)
    {
        using var source = new CancellationTokenSource();
        var token = canBeCancelled ? source.Token : CancellationToken.None;

        var pair = HedgingEngine<string>.CancellationPair.Create(token);

        if (canBeCancelled)
        {
            Assert.NotNull(pair.Registration);
        }
        else
        {
            Assert.Null(pair.Registration);
        }
    }

    private static HedgingEngineOptions<T> CreateOptions<T>()
    {
        return new HedgingEngineOptions<T>(
             2,
             HedgingTestUtilities<T>.DefaultHedgingDelayGenerator,
             ExceptionPredicates.None,
             ResultPredicates<T>.None,
             (_, _, _, _) => Task.CompletedTask);
    }

    private sealed class DummyRequestMessage
    {
        public CancellationToken CancellationToken { get; private set; }

        public void StoreCancellationToken(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        public DummyRequestMessage Clone() => new()
        {
            CancellationToken = CancellationToken
        };
    }
}
