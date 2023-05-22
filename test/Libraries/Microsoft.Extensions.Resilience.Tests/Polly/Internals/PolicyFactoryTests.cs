// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Resilience.Hedging;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Extensions.Resilience.Polly.Test.Hedging;
using Microsoft.Extensions.Resilience.Polly.Test.Helpers;
using Microsoft.Extensions.Resilience.Polly.Test.Options;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Polly.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Resilience.Polly.Test;

#pragma warning disable CS0618 // Type or member is obsolete
[Collection(nameof(ResiliencePollyFakeClockTestsCollection))]
public sealed class PolicyFactoryTests : IDisposable
{
    private const string DefaultStringResponse = "We wish you a merry Xmas!";
    private const string DefaultFallbackReturnResponse = "42";
    private const string DummyPolicyName = "Name";
    private static readonly Func<FallbackScenarioTaskArguments, Task<string>> _defaultFallbackAction = _ => Task.FromResult(DefaultFallbackReturnResponse);
    private static readonly HedgedTaskProvider<string> _defaultHedgedTaskProvider = (HedgingTaskProviderArguments _, out Task<string>? result) =>
    {
        result = Task.FromResult("test");
        return true;
    };

    private static readonly HedgedTaskProvider _defaultHedgedTaskProviderNonGeneric = (HedgingTaskProviderArguments _, out Task? result) =>
    {
        result = Task.FromResult("test");
        return true;
    };

    private readonly IPolicyFactory _policyFactory;
    private readonly FakeLogger<IPolicyFactory> _loggerMock;
    private readonly Mock<IPolicyMetering> _policyMeter;

    private readonly RetryPolicyOptions<string> _retryPolicyOptions = new() { BaseDelay = TimeSpan.FromMilliseconds(10) };
    private readonly RetryPolicyOptions _retryPolicyOptionsNonGeneric = new() { BaseDelay = TimeSpan.FromMilliseconds(10) };
    private readonly CircuitBreakerPolicyOptions<string> _defaultCircuitBreakerPolicyOptions = new();
    private readonly CircuitBreakerPolicyOptions _defaultCircuitBreakerPolicyOptionsNonGeneric = new();
    private readonly FallbackPolicyOptions<string> _defaultFallbackPolicyOptions = new();
    private readonly FallbackPolicyOptions _defaultFallbackPolicyOptionsNonGeneric = new();
    private readonly HedgingPolicyOptions<string> _defaulHedgingPolicyOptions = new();
    private readonly HedgingPolicyOptions _defaulHedgingPolicyOptionsNonGeneric = new();
    private readonly TimeSpan _cohesionTimeLimit = TimeSpan.FromMilliseconds(1000); // Consider increasing CohesionTimeLimit if bulkhead specs fail transiently in slower build environments.
    private readonly AutoResetEvent _statusChangedEvent = new(false);
    private readonly TimeSpan _shimTimeSpan = TimeSpan.FromMilliseconds(50); // How frequently to retry the assertions.

    private readonly ITestOutputHelper _output;
    private readonly FakeTimeProvider _timeProvider;

    public PolicyFactoryTests(ITestOutputHelper output)
    {
        _loggerMock = new FakeLogger<IPolicyFactory>();
        _policyMeter = new Mock<IPolicyMetering>(MockBehavior.Strict);

        var services = new ServiceCollection();
        _policyFactory = new PolicyFactory(_loggerMock, _policyMeter.Object);
        _output = output;
        _timeProvider = new FakeTimeProvider();
        SystemClock.SleepAsync = _timeProvider.DelayAndAdvanceAsync;
        SystemClock.UtcNow = () => _timeProvider.GetUtcNow().UtcDateTime;
    }

    public void Dispose()
    {
        _policyMeter.VerifyAll();
        _statusChangedEvent.Dispose();
        SystemClock.Reset();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateCircuitBreakerPolicy_WhenExecutedWithException_ShouldBreakThenReset(bool shouldHandleException)
    {
        var options = new CircuitBreakerPolicyOptions<string>
        {
            FailureThreshold = 0.1,
            MinimumThroughput = 2,
            BreakDuration = TimeSpan.FromMilliseconds(501),
            ShouldHandleException = _ => shouldHandleException
        };

        var policy = (AsyncCircuitBreakerPolicy<string>)_policyFactory.CreateCircuitBreakerPolicy("DefaultCircuitBreakerPolicy", options);
        Assert.NotNull(policy);

        if (shouldHandleException)
        {
            SetupMetering<string>("DefaultCircuitBreakerPolicy", PolicyEvents.CircuitBreakerOnBreakPolicyEvent);
            SetupMetering("DefaultCircuitBreakerPolicy", PolicyEvents.CircuitBreakerOnResetPolicyEvent);
        }

        for (int i = 0; i < 10; i++)
        {
            try
            {
                await policy.ExecuteAsync(TaskWithException);
            }
            catch (InvalidOperationException)
            {
                // Nothing
            }
            catch (BrokenCircuitException)
            {
                // Ensure that OnBreak is triggered
                break;
            }
        }

        // Make sure OnReset is triggered
        policy.Reset();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateCircuitBreakerPolicy_NonGeneric_WhenExecutedWithException_ShouldBreakThenReset(bool shouldHandleException)
    {
        var options = new CircuitBreakerPolicyOptions
        {
            FailureThreshold = 0.1,
            MinimumThroughput = 2,
            BreakDuration = TimeSpan.FromMilliseconds(501),
            ShouldHandleException = _ => shouldHandleException
        };

        var policy = (AsyncCircuitBreakerPolicy)_policyFactory.CreateCircuitBreakerPolicy("DefaultCircuitBreakerPolicy", options);
        Assert.NotNull(policy);

        if (shouldHandleException)
        {
            SetupMetering("DefaultCircuitBreakerPolicy", PolicyEvents.CircuitBreakerOnResetPolicyEvent);
            SetupMetering("DefaultCircuitBreakerPolicy", PolicyEvents.CircuitBreakerOnBreakPolicyEvent);
        }

        for (int i = 0; i < 10; i++)
        {
            try
            {
                await policy.ExecuteAsync(TaskWithExceptionNonGeneric);
            }
            catch (InvalidOperationException)
            {
                // Nothing
            }
            catch (BrokenCircuitException)
            {
                // Ensure that OnBreak is triggered
                break;
            }
        }

        // Make sure OnReset is triggered
        policy.Reset();
    }

    [Fact]
    public async Task CreateCircuitBreakerPolicy_EnsureExplicitPolicyNameRespected()
    {
        var policyName = "some-name";
        var options = new CircuitBreakerPolicyOptions<string>
        {
            FailureThreshold = 0.1,
            MinimumThroughput = 2,
            BreakDuration = TimeSpan.FromMilliseconds(501),
            ShouldHandleException = _ => true
        };

        var policy = (AsyncCircuitBreakerPolicy<string>)_policyFactory.CreateCircuitBreakerPolicy(policyName, options);

        SetupMetering<string>(policyName, PolicyEvents.CircuitBreakerOnBreakPolicyEvent);
        SetupMetering(policyName, PolicyEvents.CircuitBreakerOnResetPolicyEvent);

        for (int i = 0; i < 10; i++)
        {
            try
            {
                await policy.ExecuteAsync(TaskWithException);
            }
            catch (InvalidOperationException)
            {
                // Nothing
            }
            catch (BrokenCircuitException)
            {
                // Ensure that OnBreak is triggered
                break;
            }
        }

        // Make sure OnReset is triggered
        policy.Reset();
        _policyMeter.VerifyAll();
    }

    [Fact]
    public async Task CreateCircuitBreakerPolicy_WhenResetEvent_UsesTheCorrectOne()
    {
        var policyName = "some-name";
        var options = new CircuitBreakerPolicyOptions<string>
        {
            FailureThreshold = 0.1,
            MinimumThroughput = 2,
            BreakDuration = TimeSpan.FromMilliseconds(501),
            ShouldHandleException = _ => true
        };

        SetupMetering<string>(policyName, PolicyEvents.CircuitBreakerOnBreakPolicyEvent);
        SetupMetering(policyName, PolicyEvents.CircuitBreakerOnResetPolicyEvent);

        var policy = (AsyncCircuitBreakerPolicy<string>)_policyFactory.CreateCircuitBreakerPolicy(policyName, options);
        Assert.NotNull(policy);

        for (int i = 0; i < 10; i++)
        {
            try
            {
                await policy.ExecuteAsync(TaskWithException);
            }
            catch (InvalidOperationException)
            {
                // Nothing
            }
            catch (BrokenCircuitException)
            {
                // Ensure that OnBreak is triggered
                break;
            }
        }

        policy.Reset();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateCircuitBreakerPolicy_EnsureOnHalfOpenReported(bool generic)
    {
        var policyName = "some-name";
        var options = new CircuitBreakerPolicyOptions<string>
        {
            FailureThreshold = 0.9,
            MinimumThroughput = 2,
            BreakDuration = TimeSpan.FromMilliseconds(501),
            ShouldHandleException = _ => true
        };

        if (generic)
        {
            SetupMetering<string>(policyName, PolicyEvents.CircuitBreakerOnBreakPolicyEvent);
            SetupMetering(policyName, PolicyEvents.CircuitBreakerOnHalfOpenPolicyEvent);
            SetupMetering(policyName, PolicyEvents.CircuitBreakerOnResetPolicyEvent);
        }
        else
        {
            SetupMetering(policyName, PolicyEvents.CircuitBreakerOnBreakPolicyEvent);
            SetupMetering(policyName, PolicyEvents.CircuitBreakerOnHalfOpenPolicyEvent);
            SetupMetering(policyName, PolicyEvents.CircuitBreakerOnResetPolicyEvent);
        }

        var policy = generic ?
            _policyFactory.CreateCircuitBreakerPolicy(policyName, options) :
            _policyFactory.CreateCircuitBreakerPolicy(policyName, (CircuitBreakerPolicyOptions)options).AsAsyncPolicy<string>();

        for (int i = 0; i < 10; i++)
        {
            try
            {
                await policy.ExecuteAsync(TaskWithException);
            }
            catch (InvalidOperationException)
            {
                // Nothing
            }
            catch (BrokenCircuitException)
            {
                // Ensure that OnBreak is triggered
                break;
            }
        }

        _timeProvider.Advance(TimeSpan.FromSeconds(1));

        await policy.ExecuteAsync(() => Task.FromResult("dummy"));

        var record = _loggerMock.Collector.GetSnapshot().Single(v => v.Id == 7);
        record.Message.Should().Be("Circuit breaker policy: some-name. Half-Open has been triggered.");
    }

    [Fact]
    public async Task CreateCircuitBreakerPolicy_NonGeneric_WhenResetEvent_UsesTheCorrectOne()
    {
        var options = new CircuitBreakerPolicyOptions
        {
            FailureThreshold = 0.1,
            MinimumThroughput = 2,
            BreakDuration = TimeSpan.FromMilliseconds(501),
            ShouldHandleException = _ => true
        };

        SetupMetering("DefaultCircuitBreakerPolicy", PolicyEvents.CircuitBreakerOnBreakPolicyEvent);
        SetupMetering("DefaultCircuitBreakerPolicy", PolicyEvents.CircuitBreakerOnResetPolicyEvent);

        var policy = (AsyncCircuitBreakerPolicy)_policyFactory.CreateCircuitBreakerPolicy("DefaultCircuitBreakerPolicy", options);
        Assert.NotNull(policy);

        for (int i = 0; i < 10; i++)
        {
            try
            {
                await policy.ExecuteAsync(TaskWithException);
            }
            catch (InvalidOperationException)
            {
                // Nothing
            }
            catch (BrokenCircuitException)
            {
                // Ensure that OnBreak is triggered
                break;
            }
        }

        policy.Reset();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateCircuitBreakerPolicy_WhenExecutedWithErrorResponse_ShouldHandleResponse(bool shouldHandleResponse)
    {
        _defaultCircuitBreakerPolicyOptions.ShouldHandleResultAsError = _ => shouldHandleResponse;
        _defaultCircuitBreakerPolicyOptions.ShouldHandleException = _ => false;
        var policy = _policyFactory.CreateCircuitBreakerPolicy(DummyPolicyName, _defaultCircuitBreakerPolicyOptions);
        Assert.NotNull(policy);

        var response = await policy.ExecuteAsync(TaskWithResponse);
        Assert.Equal(DefaultStringResponse, response);
    }

    [Fact]
    public async Task CreateCircuitBreakerPolicy_NonGeneric_WhenExecutedWithErrorResponse_ShouldHandleResponse()
    {
        _defaultCircuitBreakerPolicyOptionsNonGeneric.ShouldHandleException = _ => false;
        var policy = _policyFactory.CreateCircuitBreakerPolicy("policy-name", _defaultCircuitBreakerPolicyOptionsNonGeneric);
        Assert.NotNull(policy);

        await policy.ExecuteAsync(TaskWithResponse);
    }

    [Fact]
    public void CreateCircuitBreakerPolicy_NullConfiguration_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateCircuitBreakerPolicy(null!, null!));

        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateCircuitBreakerPolicy(null!, _defaultCircuitBreakerPolicyOptions));

        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateCircuitBreakerPolicy("name", null!));
    }

    [Fact]
    public void CreateCircuitBreakerPolicy_NullConfiguration_ShouldThrow_NonGeneric()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _policyFactory.CreateCircuitBreakerPolicy("policy-name", null!));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateRetryPolicy_WhenExecutedWithException_ShouldHandleException(bool shouldHandleException)
    {
        _retryPolicyOptions.ShouldHandleException = _ => shouldHandleException;

        var policy = _policyFactory.CreateRetryPolicy("DefaultRetryPolicy", _retryPolicyOptions);
        Assert.NotNull(policy);

        if (shouldHandleException)
        {
            SetupMetering<string>("DefaultRetryPolicy", PolicyEvents.RetryPolicyEvent);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
               await policy.ExecuteAsync(TaskWithException));

        if (shouldHandleException)
        {
            Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
            Assert.Equal("LogRetry", _loggerMock.LatestRecord.Id.Name);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateRetryPolicy_NonGeneric_WhenExecutedWithException_ShouldHandleException(bool shouldHandleException)
    {
        _retryPolicyOptionsNonGeneric.ShouldHandleException = _ => shouldHandleException;

        var policy = _policyFactory.CreateRetryPolicy(DummyPolicyName, _retryPolicyOptionsNonGeneric);
        Assert.NotNull(policy);

        if (shouldHandleException)
        {
            SetupMetering(DummyPolicyName, PolicyEvents.RetryPolicyEvent);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(TaskWithException));

        if (shouldHandleException)
        {
            Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
            Assert.Equal("LogRetry", _loggerMock.LatestRecord.Id.Name);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateRetryPolicy_WhenExecutedWithErrorResponse_ShouldHandleResponse(bool shouldHandleResponse)
    {
        _retryPolicyOptions.ShouldHandleResultAsError = _ => shouldHandleResponse;
        _retryPolicyOptions.ShouldHandleException = _ => false;

        var policy = _policyFactory.CreateRetryPolicy(DummyPolicyName, _retryPolicyOptions);
        Assert.NotNull(policy);

        if (shouldHandleResponse)
        {
            SetupMetering<string>(DummyPolicyName, PolicyEvents.RetryPolicyEvent);
        }

        var response = await policy.ExecuteAsync(TaskWithResponse);
        Assert.Equal(DefaultStringResponse, response);

        if (shouldHandleResponse)
        {
            Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
            Assert.Equal("LogRetry", _loggerMock.LatestRecord.Id.Name);
        }
    }

    [Fact]
    public async Task CreateFallbackPolicy_NonGeneric_WhenExecutedWithNotHandledErrorResponse_ShouldNotHandleResponse()
    {
        _defaultFallbackPolicyOptionsNonGeneric.ShouldHandleException = _ => false;

        var policy = _policyFactory.CreateFallbackPolicy(DummyPolicyName, args => _defaultFallbackAction(args), _defaultFallbackPolicyOptionsNonGeneric);
        Assert.NotNull(policy);

        await policy.ExecuteAsync(TaskWithResponseNonGeneric);
    }

    [Fact]
    public async Task CreateRetryPolicy_EnsureExplicitPolicyNameRespected()
    {
        var policyName = "some-name";
        var options = new RetryPolicyOptions<string>
        {
            ShouldHandleException = _ => true,
            RetryCount = 1,
            ShouldHandleResultAsError = r => r == "error",
            BaseDelay = TimeSpan.Zero
        };

        var policy = _policyFactory.CreateRetryPolicy(policyName, options);

        SetupMetering<string>(policyName, PolicyEvents.RetryPolicyEvent);

        await policy.ExecuteAsync(() => Task.FromResult("error"));
        _policyMeter.VerifyAll();
    }

    [Fact]
    public async Task CreateRetryPolicy_NonGeneric_EnsureExplicitPolicyNameRespected()
    {
        var policyName = "some-name";
        var options = new RetryPolicyOptions
        {
            ShouldHandleException = _ => true,
            RetryCount = 1,
            BaseDelay = TimeSpan.Zero
        };

        var policy = _policyFactory.CreateRetryPolicy(policyName, options);

        SetupMetering(policyName, PolicyEvents.RetryPolicyEvent);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(TaskWithException));

        _policyMeter.VerifyAll();
    }

    [Theory]
    [InlineData(BackoffType.ExponentialWithJitter)]
    [InlineData(BackoffType.Constant)]
    [InlineData(BackoffType.Linear)]
    public async Task CreateRetryPolicy_WhenExecutedWithDefaultDelay(BackoffType backoffType)
    {
        _retryPolicyOptions.BaseDelay = TimeSpan.FromMilliseconds(10);
        _retryPolicyOptions.BackoffType = backoffType;

        SetupMetering<string>(DummyPolicyName, PolicyEvents.RetryPolicyEvent);

        var policy = _policyFactory.CreateRetryPolicy(DummyPolicyName, _retryPolicyOptions);
        Assert.NotNull(policy);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
               await policy.ExecuteAsync(TaskWithException));
        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogRetry", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task CreateRetryPolicy_NonGeneric_WhenExecutedWithDefaultDelay()
    {
        _retryPolicyOptions.BaseDelay = TimeSpan.FromMilliseconds(10);

        SetupMetering(DummyPolicyName, PolicyEvents.RetryPolicyEvent);

        var policy = _policyFactory.CreateRetryPolicy(DummyPolicyName, _retryPolicyOptionsNonGeneric);
        Assert.NotNull(policy);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
               await policy.ExecuteAsync(TaskWithException));
        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogRetry", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task CreateRetryPolicy_WhenExecutedWithDefaultDelayAndInfiniteRetry()
    {
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        int retryCount = 0;

        _retryPolicyOptions.BaseDelay = TimeSpan.FromTicks(1);
        _retryPolicyOptions.RetryCount = RetryPolicyOptions.InfiniteRetry;
        _retryPolicyOptions.BackoffType = BackoffType.Constant;
        _retryPolicyOptions.OnRetryAsync = (_) =>
        {
            if (++retryCount > 100)
            {
                cts.Cancel();
            }

            return Task.CompletedTask;
        };

        SetupMetering<string>("DefaultRetryPolicy", PolicyEvents.RetryPolicyEvent);

        var policy = _policyFactory.CreateRetryPolicy("DefaultRetryPolicy", _retryPolicyOptions);
        Assert.NotNull(policy);

        // .NET Framework throws either Operation or TaskCanceled exception depending on circumstances
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
               await policy.ExecuteAsync((_) => TaskWithException(), cts.Token));

        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogRetry", _loggerMock.LatestRecord.Id.Name);

        cts.Dispose();
    }

    [Fact]
    public void CreateRetryPolicy_WhenExecutedWithExponentialBackoffAndHighRetryCount_ThrowsValidationException()
    {
        _retryPolicyOptions.BaseDelay = TimeSpan.FromTicks(1);
        _retryPolicyOptions.RetryCount = 50;
        _retryPolicyOptions.BackoffType = BackoffType.ExponentialWithJitter;

        Assert.Throws<ValidationException>(() => _policyFactory.CreateRetryPolicy(DummyPolicyName, _retryPolicyOptions));
    }

    [Theory]
    [InlineData(BackoffType.ExponentialWithJitter)]
    [InlineData(BackoffType.Linear)]
    public void CreateRetryPolicy_WhenCreatedWithInfiniteRetries_ThrowsOnLinearAndExponentialBackoff(BackoffType backoffType)
    {
        _retryPolicyOptions.RetryCount = RetryPolicyOptions.InfiniteRetry;
        _retryPolicyOptions.BackoffType = backoffType;

        Assert.Throws<ValidationException>(() => _policyFactory.CreateRetryPolicy(DummyPolicyName, _retryPolicyOptions));
    }

    [Fact]
    public async Task CreateRetryPolicy_WhenExecutedWithCustomDelay()
    {
        _retryPolicyOptions.RetryDelayGenerator = (_) => TimeSpan.FromMilliseconds(5);

        SetupMetering<string>("DefaultRetryPolicy", PolicyEvents.RetryPolicyEvent);

        var policy = _policyFactory.CreateRetryPolicy("DefaultRetryPolicy", _retryPolicyOptions);
        Assert.NotNull(policy);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(TaskWithException));
        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogRetry", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task CreateRetryPolicy_RetryDelayGeneratorReturnsZeroDelay_EnsureBaseDelayUsed()
    {
        bool asserted = false;

        _retryPolicyOptions.RetryDelayGenerator = (_) => TimeSpan.Zero;
        _retryPolicyOptions.ShouldHandleResultAsError = r => true;
        _retryPolicyOptions.BaseDelay = TimeSpan.FromMinutes(12);
        _retryPolicyOptions.RetryCount = 1;
        _retryPolicyOptions.OnRetryAsync = args =>
        {
            Assert.Equal(TimeSpan.FromMinutes(12), args.WaitingTimeInterval);
            asserted = true;
            return Task.CompletedTask;
        };

        SetupMetering<string>("DefaultRetryPolicy", PolicyEvents.RetryPolicyEvent);

        var policy = _policyFactory.CreateRetryPolicy("DefaultRetryPolicy", _retryPolicyOptions);
        Assert.NotNull(policy);

        await policy.ExecuteAsync(() => Task.FromResult(string.Empty)).AdvanceTimeUntilFinished(_timeProvider);

        Assert.True(asserted);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateRetryPolicy_WithFirstDelaysOutOfRange_ThrowsValidationException(bool defaultRetryDelayGenerator)
    {
        _retryPolicyOptions.RetryDelayGenerator = defaultRetryDelayGenerator ? null : (_ => TimeSpan.FromSeconds(1));
        _retryPolicyOptions.BackoffType = BackoffType.ExponentialWithJitter;
        _retryPolicyOptions.BaseDelay = TimeSpan.FromDays(10_000_000);

        Assert.Throws<ValidationException>(() => _policyFactory.CreateRetryPolicy(DummyPolicyName, _retryPolicyOptions));
    }

    [Fact]
    public async Task CreateRetryPolicy_WhenExecutedWithCustomDelayAndInfiniteRetry()
    {
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        int retryCount = 0;

        _retryPolicyOptions.RetryDelayGenerator = (_) => TimeSpan.FromMilliseconds(5);
        _retryPolicyOptions.BackoffType = BackoffType.Constant;
        _retryPolicyOptions.RetryCount = RetryPolicyOptions.InfiniteRetry;
        _retryPolicyOptions.OnRetryAsync = (_) =>
        {
            if (++retryCount > 100)
            {
                cts.Cancel();
            }

            return Task.CompletedTask;
        };

        SetupMetering<string>("DefaultRetryPolicy", PolicyEvents.RetryPolicyEvent);

        var policy = _policyFactory.CreateRetryPolicy("DefaultRetryPolicy", _retryPolicyOptions);
        Assert.NotNull(policy);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => policy.ExecuteAsync(_ => TaskWithException(), cts.Token));

        Assert.Equal(101, retryCount);
        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogRetry", _loggerMock.LatestRecord.Id.Name);

        cts.Dispose();
    }

    [Theory]
    [InlineData(BackoffType.ExponentialWithJitter)]
    [InlineData(BackoffType.Constant)]
    [InlineData(BackoffType.Linear)]
    public async Task CreateRetryPolicy_WhenExecutedWithInvalidCustomDelay(BackoffType backoffType)
    {
        _retryPolicyOptions.RetryDelayGenerator = (_) => TimeSpan.FromMilliseconds(-5);
        _retryPolicyOptions.BackoffType = backoffType;

        SetupMetering<string>("DefaultRetryPolicy", PolicyEvents.RetryPolicyEvent);

        var policy = _policyFactory.CreateRetryPolicy("DefaultRetryPolicy", _retryPolicyOptions);
        Assert.NotNull(policy);

        // Assert it does not throw ArgumentOutOfRangeException from invalid delay
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(TaskWithException));
        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogRetry", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public void CreateRetryPolicy_NullConfiguration_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateRetryPolicy(null!, null!));

        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateRetryPolicy(null!, _retryPolicyOptions));

        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateRetryPolicy("policy-name", null!));
    }

    [Fact]
    public void CreateRetryPolicy_NonGeneric_NullConfiguration_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateRetryPolicy(null!, _retryPolicyOptionsNonGeneric));

        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateRetryPolicy("policy-name", null!));
    }

    [Fact]
    public async Task CreateRetryPolicy_NullDelayGenerators_ShouldNotThrow()
    {
        var policy = _policyFactory.CreateRetryPolicy(DummyPolicyName, _retryPolicyOptions);
        var response = await policy.ExecuteAsync(TaskWithResponse);
        Assert.Equal(DefaultStringResponse, response);
    }

    [Fact]
    public async Task CreateRetryPolicy_NonGeneric_NullDelayGenerator_ShouldNotThrow()
    {
        var policy = _policyFactory.CreateRetryPolicy(DummyPolicyName, _retryPolicyOptionsNonGeneric);
        var response = await policy.ExecuteAsync(TaskWithResponse);
        Assert.Equal(DefaultStringResponse, response);
    }

    [Fact]
    public void CreateTimeoutPolicy_WhenProperlyConfigured_ShouldInitialize()
    {
        var timeoutOptions = new TimeoutPolicyOptions
        {
            TimeoutInterval = TimeSpan.FromSeconds(1)
        };

        var policy = _policyFactory.CreateTimeoutPolicy(DummyPolicyName, timeoutOptions);
        Assert.NotNull(policy);
    }

    [Fact]
    public async Task CreateTimeoutPolicy_EnsureExplicitPolicyNameRespected()
    {
        var policyName = "some-name";
        var options = new TimeoutPolicyOptions
        {
            TimeoutInterval = TimeSpan.FromMilliseconds(1)
        };

        var policy = _policyFactory.CreateTimeoutPolicy(policyName, options);

        SetupMetering(policyName, PolicyEvents.TimeoutPolicyEvent);

        await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
            policy.ExecuteAsync(
                async (t) =>
                {
                    await _timeProvider.DelayAndAdvanceAsync(TimeSpan.FromMinutes(1), t);
                    return "result";
                },
                CancellationToken.None));
    }

    [Fact]
    public void CreateTimeoutPolicy_WhenWronglyConfigured_ShouldThrow()
    {
        var timeoutOptions = new TimeoutPolicyOptions
        {
            TimeoutInterval = TimeSpan.FromSeconds(-1)
        };

        Assert.Throws<ValidationException>(
            () => _policyFactory.CreateTimeoutPolicy(DummyPolicyName, timeoutOptions));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateTimeoutPolicy_WhenExecutedWithTimeout_ShouldTrackEvent(bool useOldEvent)
    {
        SetupMetering("DefaultTimeoutPolicy", PolicyEvents.TimeoutPolicyEvent);

        var timeoutOptions = new TimeoutPolicyOptions
        {
            TimeoutInterval = TimeSpan.FromSeconds(1),
            TimeoutStrategy = TimeoutStrategy.Pessimistic
        };

        if (!useOldEvent)
        {
            timeoutOptions.OnTimedOutAsync = _ => Task.FromResult(true);
        }

        var policy = _policyFactory.CreateTimeoutPolicy("DefaultTimeoutPolicy", timeoutOptions);
        await Assert.ThrowsAsync<TimeoutRejectedException>(
            () => policy.ExecuteAsync(
               async () =>
               {
                   // Timeout policy based on cancellation tokens
                   await Task.Delay(TimeSpan.FromSeconds(10));
                   return "lala";
               }));

        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogTimeout", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public void CreateTimeoutPolicy_NullConfiguration_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateTimeoutPolicy(null!, null!));

        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateTimeoutPolicy(null!, new TimeoutPolicyOptions()));

        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateRetryPolicy("policy-name", null!));
    }

    [Fact]
    public async Task CreateFallbackPolicy_WhenExecutedWithNonHandledException_ShouldNoHandleException()
    {
        _defaultFallbackPolicyOptions.ShouldHandleException = _ => false;

        var policy = _policyFactory.CreateFallbackPolicy(DummyPolicyName, args => _defaultFallbackAction(args), _defaultFallbackPolicyOptions);
        Assert.NotNull(policy);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
               await policy.ExecuteAsync(TaskWithException));
    }

    [Fact]
    public async Task CreateFallbackPolicy_NonGeneric_WhenExecutedWithNonHandledException_ShouldNoHandleException()
    {
        _defaultFallbackPolicyOptionsNonGeneric.ShouldHandleException = _ => false;

        var policy = _policyFactory.CreateFallbackPolicy(DummyPolicyName, args => _defaultFallbackAction(args), _defaultFallbackPolicyOptionsNonGeneric);
        Assert.NotNull(policy);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(TaskWithException));
    }

    [Fact]
    public async Task CreateFallbackPolicy_WhenExecutedWithHandledException_ShouldHandleException()
    {
        SetupMetering<string>(DummyPolicyName, PolicyEvents.FallbackPolicyEvent);

        var policy = _policyFactory.CreateFallbackPolicy(
            DummyPolicyName,
            args => _defaultFallbackAction(args),
            _defaultFallbackPolicyOptions);
        Assert.NotNull(policy);

        var result = await policy.ExecuteAsync(TaskWithException);
        Assert.Equal(DefaultFallbackReturnResponse, result);
        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogFallback", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task CreateFallbackPolicy_NonGeneric_WhenExecutedWithHandledException_ShouldHandleException()
    {
        SetupMetering(DummyPolicyName, PolicyEvents.FallbackPolicyEvent);

        var policy = _policyFactory.CreateFallbackPolicy(
            DummyPolicyName,
            args => _defaultFallbackAction(args),
            _defaultFallbackPolicyOptionsNonGeneric);
        Assert.NotNull(policy);

        await policy.ExecuteAsync(TaskWithExceptionNonGeneric);
        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogFallback", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task CreateFallback_EnsureExplicitPolicyNameRespected()
    {
        var policyName = "some-name";
        var options = new TimeoutPolicyOptions
        {
            TimeoutInterval = TimeSpan.FromMilliseconds(1)
        };

        var policy = _policyFactory.CreateFallbackPolicy(policyName, _ => Task.FromResult(DefaultFallbackReturnResponse), _defaultFallbackPolicyOptions);

        SetupMetering<string>(policyName, PolicyEvents.FallbackPolicyEvent);

        var result = await policy.ExecuteAsync(TaskWithException);

        _policyMeter.VerifyAll();
    }

    [Fact]
    public async Task CreateFallback_NonGeneric_EnsureExplicitPolicyNameRespected()
    {
        var policyName = "some-name";
        var policy = _policyFactory.CreateFallbackPolicy(policyName, _ => Task.FromResult(DefaultFallbackReturnResponse), _defaultFallbackPolicyOptionsNonGeneric);

        SetupMetering(policyName, PolicyEvents.FallbackPolicyEvent);

        await policy.ExecuteAsync(TaskWithExceptionNonGeneric);

        _policyMeter.VerifyAll();
    }

    [Fact]
    public async Task ObsoleteCreateFallbackPolicy_WhenExecutedWithHandledException_ShouldHandleException()
    {
        SetupMetering<string>(DummyPolicyName, PolicyEvents.FallbackPolicyEvent);

        var policy = _policyFactory.CreateFallbackPolicy(
            DummyPolicyName,
            (_) => Task.FromResult(DefaultFallbackReturnResponse),
            _defaultFallbackPolicyOptions);
        Assert.NotNull(policy);

        var result = await policy.ExecuteAsync(TaskWithException);
        Assert.Equal(DefaultFallbackReturnResponse, result);
        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogFallback", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task ObsoleteCreateFallbackPolicy_NonGeneric_WhenExecutedWithHandledException_ShouldHandleException()
    {
        SetupMetering(DummyPolicyName, PolicyEvents.FallbackPolicyEvent);

        var policy = _policyFactory.CreateFallbackPolicy(
            DummyPolicyName,
            _ => Task.FromResult(DefaultFallbackReturnResponse),
            _defaultFallbackPolicyOptionsNonGeneric);
        Assert.NotNull(policy);

        await policy.ExecuteAsync(TaskWithExceptionNonGeneric);
        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogFallback", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task CreateFallbackPolicy_WhenExecutedWithNotHandledErrorResponse_ShouldNotHandleResponse()
    {
        _defaultFallbackPolicyOptions.ShouldHandleException = _ => false;

        var policy = _policyFactory.CreateFallbackPolicy(DummyPolicyName, args => _defaultFallbackAction(args), _defaultFallbackPolicyOptions);
        Assert.NotNull(policy);

        var response = await policy.ExecuteAsync(TaskWithResponse);
        Assert.Equal(DefaultStringResponse, response);
    }

    [Fact]
    public async Task CreateFallbackPolicy_WhenExecutedWithHandledErrorResponse_ShouldHandleResponse()
    {
        _defaultFallbackPolicyOptions.ShouldHandleResultAsError = _ => true;
        _defaultFallbackPolicyOptions.ShouldHandleException = _ => false;

        SetupMetering<string>(DummyPolicyName, PolicyEvents.FallbackPolicyEvent);

        var policy = _policyFactory.CreateFallbackPolicy(DummyPolicyName, args => _defaultFallbackAction(args), _defaultFallbackPolicyOptions);
        Assert.NotNull(policy);

        var result = await policy.ExecuteAsync(TaskWithResponse);
        Assert.Equal(DefaultFallbackReturnResponse, result);
        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogFallback", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task CreateFallbackPolicy_WhenExecutedForDisposableObject_ShouldDispose()
    {
        SetupMetering<CustomObject>(DummyPolicyName, PolicyEvents.FallbackPolicyEvent);

        var options = new FallbackPolicyOptions<CustomObject>
        {
            ShouldHandleResultAsError = _ => true,
            ShouldHandleException = _ => false
        };

        var fallbackResult = new CustomObject("fallbackResult");
        var policy = _policyFactory.CreateFallbackPolicy<CustomObject>(
            DummyPolicyName,
            _ => Task.FromResult(fallbackResult),
            options);
        Assert.NotNull(policy);

        var initialResult = new CustomObject("initialResult");
        var result = await policy.ExecuteAsync(() => Task.FromResult(initialResult));

        Assert.Equal(fallbackResult, result);
        Assert.Null(initialResult.Content);

        fallbackResult.Dispose();
        initialResult.Dispose();
    }

    [Fact]
    public void CreateFallbackPolicy_NullFallbackAction_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _policyFactory.CreateFallbackPolicy(DummyPolicyName, null!, _defaultFallbackPolicyOptions));
    }

    [Fact]
    public void CreateFallbackPolicy_NullOptions_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateFallbackPolicy(null!, args => _defaultFallbackAction(args), null!));
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateFallbackPolicy(null!, arg => _defaultFallbackAction(arg), new FallbackPolicyOptions<string>()));
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateFallbackPolicy("name", arg => _defaultFallbackAction(arg), null!));
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateFallbackPolicy("name", null!, new FallbackPolicyOptions<string>()));
    }

    [Fact]
    public async Task CreateBulkheadPolicy_WithDefaultsWhenExecutedWithoutBulkhead_ShouldSucceed()
    {
        var policy = _policyFactory.CreateBulkheadPolicy(DummyPolicyName, Constants.BulkheadPolicy.DefaultOptions);

        var initialResult = "initialResult";
        var result = await policy.ExecuteAsync(() => Task.FromResult(initialResult));
        Assert.Equal(initialResult, result);
    }

    [Fact]
    public async Task CreateBulkheadPolicy_WhenExecutedWithBulkhead_ShouldThrowRejection()
    {
        string operationKey = "SomeKey";
        Context contextPassedToExecute = new Context(operationKey);
        Context? contextPassedToOnRejected = null;
        var options = new BulkheadPolicyOptions
        {
            MaxConcurrency = 1,
            MaxQueuedActions = 0,
            OnBulkheadRejectedAsync = args =>
            {
                contextPassedToOnRejected = args.Context;
                return Task.CompletedTask;
            }
        };

        SetupMetering(DummyPolicyName, "BulkheadPolicy-OnBulkheadRejected");

        var policy = (AsyncBulkheadPolicy)_policyFactory.CreateBulkheadPolicy(DummyPolicyName, options)!;
        Assert.NotNull(policy);

        TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
        using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() =>
            {
                _ = policy.ExecuteAsync(async () =>
                {
                    await tcs.Task;
                    return string.Empty;
                });
            }).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            Within(_cohesionTimeLimit, () => Expect(0, () => policy.BulkheadAvailableCount, nameof(policy.BulkheadAvailableCount)));

            await Assert.ThrowsAsync<BulkheadRejectedException>(async () => await policy.ExecuteAsync(_ => Task.FromResult("x"), contextPassedToExecute));

            cancellationSource.Cancel();
            tcs.SetCanceled();
        }

        Assert.NotNull(contextPassedToOnRejected);
        Assert.Equal(operationKey, contextPassedToOnRejected!.OperationKey);
        Assert.Equal(contextPassedToExecute, contextPassedToOnRejected);

        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogBulkhead", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task CreateBulkhead_EnsureExplicitPolicyNameRespected()
    {
        var policyName = "some-name";
        var options = new BulkheadPolicyOptions
        {
            MaxConcurrency = 1,
            MaxQueuedActions = 0
        };
        var policy = _policyFactory.CreateBulkheadPolicy(policyName, options);
        using var cts = new CancellationTokenSource();

        SetupMetering(policyName, "BulkheadPolicy-OnBulkheadRejected");

        var t = policy.ExecuteAsync(LongTask, cts.Token);
        await Assert.ThrowsAsync<BulkheadRejectedException>(() => policy.ExecuteAsync(LongTask, cts.Token));
        cts.Cancel();

        try
        {
            await t;
        }
        catch (OperationCanceledException)
        {
            // suppress
        }

        static async Task<string> LongTask(CancellationToken token)
        {
            await Task.Delay(TimeSpan.FromDays(1), token);
            return "test";
        }
    }

    [Fact]
    public void CreateBulkheadPolicyWithDefaultMaxConcurrency()
    {
        var policy = _policyFactory.CreateBulkheadPolicy(DummyPolicyName, Constants.BulkheadPolicy.DefaultOptions);
        Assert.NotNull(policy);
    }

    [Fact]
    public void CreateBulkheadPolicy_NullConfiguration()
    {
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateBulkheadPolicy(null!, null!));
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateBulkheadPolicy(null!, new BulkheadPolicyOptions()));
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateBulkheadPolicy("name", null!));
    }

    [Fact]
    public async Task CreateHedgingPolicy_WhenExecutedWithNonHandledException_ShouldNoHandleException()
    {
        _defaulHedgingPolicyOptions.ShouldHandleException = _ => false;

        var policy = _policyFactory.CreateHedgingPolicy(
            DummyPolicyName,
           HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProvider,
           _defaulHedgingPolicyOptions);
        Assert.NotNull(policy);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
               await policy.ExecuteAsync(TaskWithException));
    }

    [Fact]
    public async Task CreateHedgingPolicy_NonGeneric_WhenExecutedWithNonHandledException_ShouldNoHandleException()
    {
        _defaulHedgingPolicyOptionsNonGeneric.ShouldHandleException = _ => false;

        var policy = _policyFactory.CreateHedgingPolicy(
           "HedgingPolicy",
           HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.FunctionsProviderNonGeneric,
           _defaulHedgingPolicyOptionsNonGeneric);
        Assert.NotNull(policy);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
               await policy.ExecuteAsync(TaskWithException));
    }

    [Fact]
    public async Task CreateHedgingPolicy_WhenExecutedWithHandledException_ShouldHandleException()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        SystemClock.SleepAsync = fakeTimeProvider.DelayAndAdvanceAsync;

        SetupMetering<string>(DummyPolicyName, PolicyEvents.HedgingPolicyEvent);

        var options = new HedgingPolicyOptions<string>
        {
            MaxHedgedAttempts = HedgingTestUtilities<string>.HedgedTasksHandler.MaxHedgedTasks,
            HedgingDelay = HedgingTestUtilities<string>.DefaultHedgingDelay
        };

        var policy = (_policyFactory.CreateHedgingPolicy(
            DummyPolicyName,
            HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProvider,
            options) as AsyncHedgingPolicy<string>)!;
        Assert.NotNull(policy);

        var result = await policy.ExecuteAsync(TaskWithException).AdvanceTimeUntilFinished(_timeProvider);

        Assert.Contains(result,
            new[]
            {
                "Oranges", "Pears", "Apples"
            });

        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogHedging", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task CreateHedgingPolicy_NonGeneric_WhenExecutedWithHandledException_ShouldHandleException()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        SystemClock.SleepAsync = fakeTimeProvider.DelayAndAdvanceAsync;

        SetupMetering(DummyPolicyName, PolicyEvents.HedgingPolicyEvent);

        var options = new HedgingPolicyOptions
        {
            MaxHedgedAttempts = HedgingTestUtilities<string>.HedgedTasksHandler.MaxHedgedTasks,
            HedgingDelay = HedgingTestUtilities<string>.DefaultHedgingDelay
        };

        var policy = (_policyFactory.CreateHedgingPolicy(
            DummyPolicyName,
            HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.FunctionsProviderNonGeneric,
            options) as AsyncHedgingPolicy)!;
        Assert.NotNull(policy);

        await policy.ExecuteAsync(TaskWithException);

        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogHedging", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task CreateHedgingPolicy_WhenExecutedWithNotHandledErrorResponse_ShouldNotHandleResponse()
    {
        _defaulHedgingPolicyOptions.ShouldHandleException = _ => false;

        var policy = _policyFactory.CreateHedgingPolicy(
            DummyPolicyName,
            HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProvider,
            _defaulHedgingPolicyOptions);
        Assert.NotNull(policy);

        var response = await policy.ExecuteAsync(TaskWithResponse).AdvanceTimeUntilFinished(_timeProvider);
        Assert.Equal(DefaultStringResponse, response);
    }

    [Fact]
    public async Task CreateHedgingPolicy_WhenExecutedWithHandledErrorResponse_ShouldHandleResponse()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        SystemClock.SleepAsync = fakeTimeProvider.DelayAndAdvanceAsync;

        SetupMetering<string>(DummyPolicyName, PolicyEvents.HedgingPolicyEvent);

        var options = new HedgingPolicyOptions<string>
        {
            MaxHedgedAttempts = HedgingTestUtilities<string>.HedgedTasksHandler.MaxHedgedTasks,
            HedgingDelay = HedgingTestUtilities<string>.DefaultHedgingDelay,
            ShouldHandleException = _ => true,
            ShouldHandleResultAsError = _ => true
        };

        var policy = (_policyFactory.CreateHedgingPolicy(
            DummyPolicyName,
            HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProvider,
            options) as AsyncHedgingPolicy<string>)!;
        Assert.NotNull(policy);

        var result = await policy.ExecuteAsync(TaskWithResponse).AdvanceTimeUntilFinished(fakeTimeProvider);

        Assert.Contains(result,
            new[]
            {
                "Oranges", "Pears", "Apples",
                DefaultStringResponse
            });
        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogHedging", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public async Task CreateHedgingPolicy_NonGeneric_WhenExecutedWithHandledErrorResponse_ShouldHandleResponse()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        SystemClock.SleepAsync = fakeTimeProvider.DelayAndAdvanceAsync;

        SetupMetering("Name", PolicyEvents.HedgingPolicyEvent);

        var options = new HedgingPolicyOptions
        {
            MaxHedgedAttempts = HedgingTestUtilities<string>.HedgedTasksHandler.MaxHedgedTasks,
            HedgingDelay = HedgingTestUtilities<string>.DefaultHedgingDelay,
            ShouldHandleException = _ => true
        };

        var policy = (_policyFactory.CreateHedgingPolicy(
            DummyPolicyName,
            HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.FunctionsProviderNonGeneric,
            options) as AsyncHedgingPolicy)!;
        Assert.NotNull(policy);

        await policy.ExecuteAsync(() => Task.FromException(new ArgumentNullException()));

        Assert.Equal(LogLevel.Warning, _loggerMock.LatestRecord.Level);
        Assert.Equal("LogHedging", _loggerMock.LatestRecord.Id.Name);
    }

    [Fact]
    public void CreateHedgingPolicy_NullArguments_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _policyFactory.CreateHedgingPolicy(
                DummyPolicyName,
                null!,
                Constants.HedgingPolicy.DefaultOptions<string>()));

        Assert.Throws<ArgumentNullException>(() =>
            _policyFactory.CreateHedgingPolicy(
                DummyPolicyName,
                HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProvider,
                null!));

        Assert.Throws<ArgumentNullException>(() =>
            _policyFactory.CreateHedgingPolicy(
                DummyPolicyName,
                null!,
                Constants.HedgingPolicy.DefaultOptions<string>()));
    }

    [Fact]
    public void CreateHedgingPolicy_NonGeneric_NullArguments_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _policyFactory.CreateHedgingPolicy(
                "HedgingPolicy",
                null!,
                Constants.HedgingPolicyNonGeneric.DefaultOptions()));

        Assert.Throws<ArgumentNullException>(() =>
            _policyFactory.CreateHedgingPolicy(
                "HedgingPolicy",
                HedgingTestUtilities<EmptyStruct>.HedgedTasksHandler.FunctionsProviderNonGeneric,
                null!));

        Assert.Throws<ArgumentNullException>(() =>
            _policyFactory.CreateHedgingPolicy(
                "HedgingPolicy",
                null!,
                Constants.HedgingPolicyNonGeneric.DefaultOptions()));
    }

    [Fact]
    public async Task CreateHedging_EnsureExplicitPolicyNameRespected()
    {
        var policyName = "some-name";
        var options = new HedgingPolicyOptions<string>
        {
            ShouldHandleResultAsError = e => e == "error"
        };
        var policy = _policyFactory.CreateHedgingPolicy(policyName, _defaultHedgedTaskProvider, options);
        using var cts = new CancellationTokenSource();

        SetupMetering<string>(policyName, PolicyEvents.HedgingPolicyEvent);

        await policy.ExecuteAsync(() => Task.FromResult("error"));
        _policyMeter.VerifyAll();
    }

    [Fact]
    public async Task CreateHedging_NonGeneric_EnsureExplicitPolicyNameRespected()
    {
        var policyName = "some-name";
        var options = new HedgingPolicyOptions();
        var policy = _policyFactory.CreateHedgingPolicy(policyName, _defaultHedgedTaskProviderNonGeneric, options);
        using var cts = new CancellationTokenSource();

        SetupMetering(policyName, PolicyEvents.HedgingPolicyEvent);

        await policy.ExecuteAsync(() => Task.FromException(new ArgumentNullException()));

        _policyMeter.VerifyAll();
    }

    [Fact]
    public void CreateHedgingPolicy_NullConfiguration()
    {
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateHedgingPolicy(DummyPolicyName, _defaultHedgedTaskProvider, null!));
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateHedgingPolicy(DummyPolicyName, null!, _defaulHedgingPolicyOptions));

        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateHedgingPolicy("name", _defaultHedgedTaskProvider, null!));
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateHedgingPolicy("name", null!, _defaulHedgingPolicyOptions));
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateHedgingPolicy(null!, _defaultHedgedTaskProvider, _defaulHedgingPolicyOptions));
    }

    [Fact]
    public void CreateHedgingPolicy_NonGeneric_NullConfiguration()
    {
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateHedgingPolicy("HedgingPolicy", _defaultHedgedTaskProviderNonGeneric, null!));
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateHedgingPolicy("HedgingPolicy", null!, _defaulHedgingPolicyOptionsNonGeneric));

        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateHedgingPolicy("name", _defaultHedgedTaskProviderNonGeneric, null!));
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateHedgingPolicy("name", null!, _defaulHedgingPolicyOptionsNonGeneric));
        Assert.Throws<ArgumentNullException>(() => _policyFactory.CreateHedgingPolicy(null!, _defaultHedgedTaskProviderNonGeneric, _defaulHedgingPolicyOptionsNonGeneric));
    }

    [Fact]
    public void SetPipelineIdentifiers_EnsureMeterInitialized()
    {
        var id = PipelineId.Create<string>("pipeline", "key");

        _policyMeter.Setup(v => v.Initialize(id));
        _policyFactory.Initialize(id);

        _policyMeter.VerifyAll();
    }

    private static Task<string> TaskWithResponse()
    {
        return Task.FromResult(DefaultStringResponse);
    }

    private static Task TaskWithResponseNonGeneric()
    {
        return Task.FromResult(DefaultStringResponse);
    }

    private static Task<string> TaskWithException()
    {
        throw new InvalidOperationException("Something went wrong");
    }

    private static Task TaskWithExceptionNonGeneric()
    {
        throw new InvalidOperationException("Something went wrong");
    }

    private static AssertionFailure? Expect(int expected, Func<int> actualFunc, string measure)
    {
        int actual = actualFunc();
        return actual != expected ? new AssertionFailure(expected, actual, measure) : null;
    }

    private void SetupMetering<T>(string policyName, string eventName)
    {
        _policyMeter.Setup(x => x.RecordEvent(
            policyName,
            eventName,
            It.IsAny<DelegateResult<T>>(),
            It.IsAny<Context>()));
    }

    private void SetupMetering(string policyName, string eventName)
    {
        _policyMeter.Setup(x => x.RecordEvent(
            policyName,
            eventName,
            It.IsAny<Exception>(),
            It.IsAny<Context>()));
    }

    private void Within(TimeSpan timeSpan, Func<AssertionFailure?> actionContainingAssertions)
    {
        TimeSpan permitted = timeSpan;
        Stopwatch watch = Stopwatch.StartNew();
        while (true)
        {
            var potentialFailure = actionContainingAssertions();
            if (potentialFailure == null)
            {
                break;
            }

            if (watch.Elapsed > permitted)
            {
                _output.WriteLine("Failing assertion on: {0}", potentialFailure.Measure);
                Assert.Equal(potentialFailure.Expected, potentialFailure.Actual);
                throw new InvalidOperationException("Code should never reach here. Preceding assertion should fail.");
            }

            bool signaled = _statusChangedEvent.WaitOne(_shimTimeSpan);
            if (signaled)
            {
                // Following TraceableAction.CaptureCompletion() signalling the AutoResetEvent,
                // there can be race conditions between on the one hand exiting the bulkhead semaphore (and potentially another execution gaining it),
                // and the assertion being verified here about those same facts.
                // If that race is lost by the real-world state change, and the AutoResetEvent signal occurred very close to timeoutTime,
                // there might not be a second chance.
                // We therefore permit another shim time for the condition to come good.
                permitted += _cohesionTimeLimit;
            }
        }
    }
}
