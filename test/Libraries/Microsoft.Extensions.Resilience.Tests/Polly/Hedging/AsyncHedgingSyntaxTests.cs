// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Hedging;
using Microsoft.Extensions.Time.Testing;
using Polly;
using Polly.Utilities;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Hedging;

[Collection(nameof(ResiliencePollyFakeClockTestsCollection))]
public sealed class AsyncHedgingSyntaxTests : IDisposable
{
    private readonly Context _context;
    private readonly CancellationTokenSource _cts;
    private readonly FakeTimeProvider _timeProvider;

    public AsyncHedgingSyntaxTests()
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
    public async Task AsyncHedgingPolicy_AllRequiredArgs_ShouldCreatePolicy()
    {
        var hedgingPolicy = Policy
            .Handle<Exception>()
            .OrResult<string>(_ => false)
            .AsyncHedgingPolicy(
                HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProvider,
                1 + HedgingTestUtilities<string>.HedgedTasksHandler.Functions.Count,
                HedgingTestUtilities<string>.DefaultHedgingDelayGenerator,
                HedgingTestUtilities<string>.EmptyOnHedgingTask);

        Assert.NotNull(hedgingPolicy);

        var result = await hedgingPolicy.ExecuteAsync(
            () =>
            HedgingTestUtilities<string>.PrimaryStringTasks.FastTask(_cts.Token));

        Assert.Contains(result,
            new[]
            {
                "Oranges", "Pears", "Apples",
                 HedgingTestUtilities<string>.PrimaryStringTasks.FastTaskResult
            });
    }

    [Fact]
    public async Task AsyncHedgingPolicy_AllRequiredArgs_ShouldCreatePolicy_EmptyStruct()
    {
        var hedgingPolicy = Policy
            .Handle<Exception>()
            .OrResult<EmptyStruct>(_ => false)
            .AsyncHedgingPolicy(
                HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.FunctionsProvider,
                1 + HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.Functions.Count,
                HedgingTestUtilities<EmptyStruct>.DefaultHedgingDelayGenerator,
                HedgingTestUtilities<EmptyStruct>.EmptyOnHedgingTask);

        Assert.NotNull(hedgingPolicy);

        var result = await hedgingPolicy.ExecuteAsync(
            () =>
            HedgingTestUtilities<EmptyStruct>.PrimaryStringTasks.GenericFastTask(EmptyStruct.Instance, _cts.Token));

        Assert.Equal(result, EmptyStruct.Instance);
    }

    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [Theory]
    public void WrapProvider_Ok(bool nullTask, bool result, bool expectedResult)
    {
        var provider = AsyncHedgingSyntax.WrapProvider(Provider);

        Assert.Equal(expectedResult, provider(default, out var wrappedResult));
        if (expectedResult)
        {
            Assert.NotNull(wrappedResult);
        }
        else
        {
            Assert.Null(wrappedResult);
        }

        bool Provider(HedgingTaskProviderArguments args, out Task? task)
        {
            task = nullTask ? null : Task.CompletedTask;
            return result;
        }
    }
}
