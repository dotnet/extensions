// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Polly;
using Polly.Retry;
using Polly.Telemetry;
using Polly.Testing;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Polly;

public class HttpRetryResiliencePipelineBuilderExtensionsTests
{
    private readonly List<TelemetryEventArguments<object, object>> _events = [];
    private int _attempts;

    [Fact]
    public void AddHttpRetry_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => HttpRetryResiliencePipelineBuilderExtensions.AddHttpRetry(null!, new HttpRetryStrategyOptions()));
        Assert.Throws<ArgumentNullException>(() => new ResiliencePipelineBuilder<HttpResponseMessage>().AddHttpRetry(null!));
        Assert.Throws<ArgumentNullException>(() => HttpRetryResiliencePipelineBuilderExtensions.AddHttpRetry(null!, HttpRetryPreset.Budgeted));
    }

    [Fact]
    public void AddHttpRetry_UndefinedPreset_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResiliencePipelineBuilder<HttpResponseMessage>().AddHttpRetry((HttpRetryPreset)42));
    }

    [Theory]
    [InlineData(HttpRetryPreset.Standard, 1, 3)]
    [InlineData(HttpRetryPreset.Budgeted, 2, 2)]
    public void AddHttpRetry_Preset_AddsExpectedStrategies(HttpRetryPreset retryPreset, int strategyCount, int maxRetryAttempts)
    {
        var descriptor = new ResiliencePipelineBuilder<HttpResponseMessage>().AddHttpRetry(retryPreset).Build().GetPipelineDescriptor();

        descriptor.Strategies.Should().HaveCount(strategyCount);
        var options = descriptor.Strategies.Last().Options.Should().BeOfType<HttpRetryStrategyOptions>().Which;
        options.MaxRetryAttempts.Should().Be(maxRetryAttempts);
        options.ShouldRetryAfterHeader.Should().BeTrue();
        options.BackoffType.Should().Be(DelayBackoffType.Exponential);
        options.UseJitter.Should().BeTrue();

        if (retryPreset == HttpRetryPreset.Budgeted)
        {
            descriptor.Strategies[0].Options.Should().BeOfType<RetryBudgetStrategyOptions>();
            options.RetryBudget.Should().BeEquivalentTo(new HttpRetryBudgetOptions());
        }
        else
        {
            options.RetryBudget.Should().BeNull();
        }
    }

    [Fact]
    public void AddHttpRetry_NoBudget_AddsRetryOnly()
    {
        var options = new HttpRetryStrategyOptions();

        var descriptor = new ResiliencePipelineBuilder<HttpResponseMessage>().AddHttpRetry(options).Build().GetPipelineDescriptor();

        descriptor.Strategies.Should().ContainSingle();
        descriptor.Strategies[0].Options.Should().BeSameAs(options);
    }

    [Fact]
    public void AddHttpRetry_WithBudget_AddsBudgetBeforeRetry()
    {
        var options = new HttpRetryStrategyOptions { Name = "my-retry", RetryBudget = new() };

        var descriptor = new ResiliencePipelineBuilder<HttpResponseMessage>().AddHttpRetry(options).Build().GetPipelineDescriptor();

        descriptor.Strategies.Should().HaveCount(2);
        descriptor.Strategies[0].Options.Should().BeOfType<RetryBudgetStrategyOptions>().Which.Name.Should().Be("my-retry");
        descriptor.Strategies[1].Options.Should().BeOfType<HttpRetryStrategyOptions>().Which.Should().NotBeSameAs(options);
    }

    [Fact]
    public void AddHttpRetry_WithBudget_DoesNotMutateOptions()
    {
        var options = new HttpRetryStrategyOptions { RetryBudget = new() };
        var shouldHandle = options.ShouldHandle;

        _ = new ResiliencePipelineBuilder<HttpResponseMessage>().AddHttpRetry(options).Build();

        options.ShouldHandle.Should().BeSameAs(shouldHandle);
    }

    [Fact]
    public void AddHttpRetry_InvalidBudget_Throws()
    {
        var options = new HttpRetryStrategyOptions { Name = "my-retry", RetryBudget = new() { RetryRatio = 2 } };

        var exception = Assert.Throws<ValidationException>(() => new ResiliencePipelineBuilder<HttpResponseMessage>().AddHttpRetry(options));

        exception.Message.Should().Contain("RetryRatio");
    }

    [Fact]
    public void CreateBudgetedCopy_CopiesAllProperties()
    {
        Func<OnRetryArguments<HttpResponseMessage>, ValueTask> onRetry = _ => default;
        Func<RetryDelayGeneratorArguments<HttpResponseMessage>, ValueTask<TimeSpan?>> delayGenerator = _ => default;
        Func<double> randomizer = () => 0.5;
        var options = new HttpRetryStrategyOptions
        {
            Name = "my-retry",
            MaxRetryAttempts = 7,
            BackoffType = DelayBackoffType.Linear,
            UseJitter = false,
            Delay = TimeSpan.FromSeconds(3),
            MaxDelay = TimeSpan.FromSeconds(9),
            ShouldRetryAfterHeader = false,
            DelayGenerator = delayGenerator,
            OnRetry = onRetry,
            Randomizer = randomizer,
            RetryBudget = new(),
        };

        var copy = HttpRetryResiliencePipelineBuilderExtensions.CreateBudgetedCopy(options);

        // Fails when a property is added to the options, so that the copy is updated too.
        var properties = typeof(HttpRetryStrategyOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite).ToList();
        properties.Select(p => p.Name).Should().BeEquivalentTo(
            "Name",
            "MaxRetryAttempts",
            "BackoffType",
            "UseJitter",
            "Delay",
            "MaxDelay",
            "ShouldRetryAfterHeader",
            "DelayGenerator",
            "OnRetry",
            "Randomizer",
            "RetryBudget",
            "ShouldHandle");

        foreach (var property in properties.Where(p => p.Name != nameof(HttpRetryStrategyOptions.ShouldHandle)))
        {
            property.GetValue(copy).Should().Be(property.GetValue(options), because: property.Name);
        }

        copy.ShouldHandle.Should().NotBeSameAs(options.ShouldHandle);
    }

    [Fact]
    public void CreateBudgetedCopy_KeepsRetryAfterDelayGenerator()
    {
        var options = new HttpRetryStrategyOptions();

        var copy = HttpRetryResiliencePipelineBuilderExtensions.CreateBudgetedCopy(options);

        copy.ShouldRetryAfterHeader.Should().BeTrue();
        copy.DelayGenerator.Should().BeSameAs(options.DelayGenerator);
    }

    [Fact]
    public async Task Execute_BudgetExhausted_StopsRetryingAndReportsEvent()
    {
        var pipeline = CreatePipeline(new() { RetryRatio = 0.5, MinimumThroughput = 1 });

        using var response = await pipeline.ExecuteAsync(_ => FailAsync());

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        _attempts.Should().Be(2);

        var budgetEvent = _events.Should().ContainSingle(e => e.Event.EventName == RetryBudgetStrategy.BudgetExhaustedEventName).Subject;
        budgetEvent.Event.Severity.Should().Be(ResilienceEventSeverity.Warning);
        budgetEvent.Source.StrategyName.Should().Be("my-retry");
        budgetEvent.Arguments.Should().BeOfType<OnRetryBudgetExhaustedArguments>().Which.AttemptNumber.Should().Be(1);
        budgetEvent.Outcome!.Value.Result.Should().BeOfType<HttpResponseMessage>();
    }

    [Fact]
    public async Task Execute_BelowMinimumThroughput_RetriesUpToMaxRetryAttempts()
    {
        var pipeline = CreatePipeline(new() { RetryRatio = 0.1, MinimumThroughput = 100 });

        using var response = await pipeline.ExecuteAsync(_ => FailAsync());

        _attempts.Should().Be(4);
        _events.Should().NotContain(e => e.Event.EventName == RetryBudgetStrategy.BudgetExhaustedEventName);
    }

    [Fact]
    public async Task Execute_ZeroRatio_NeverRetries()
    {
        var pipeline = CreatePipeline(new() { RetryRatio = 0 });

        using var response = await pipeline.ExecuteAsync(_ => FailAsync());

        _attempts.Should().Be(1);
        _events.Should().ContainSingle(e => e.Event.EventName == RetryBudgetStrategy.BudgetExhaustedEventName);
    }

    [Fact]
    public async Task Execute_LastAttempt_DoesNotConsumeBudget()
    {
        var pipeline = CreatePipeline(new() { RetryRatio = 1, MinimumThroughput = 1 }, maxRetryAttempts: 1);

        using var first = await pipeline.ExecuteAsync(_ => FailAsync());
        using var second = await pipeline.ExecuteAsync(_ => FailAsync());

        _attempts.Should().Be(4);
        _events.Should().NotContain(e => e.Event.EventName == RetryBudgetStrategy.BudgetExhaustedEventName);
    }

    [Fact]
    public async Task Execute_NotHandledOutcome_DoesNotConsumeBudget()
    {
        var pipeline = CreatePipeline(new() { RetryRatio = 0.5, MinimumThroughput = 1 });

        using var okResponse = new HttpResponseMessage(HttpStatusCode.OK);
        for (int i = 0; i < 10; i++)
        {
            _ = await pipeline.ExecuteAsync(_ => new ValueTask<HttpResponseMessage>(okResponse));
        }

        _attempts = 0;
        using var response = await pipeline.ExecuteAsync(_ => FailAsync());

        // 11 requests allow 5 retries.
        _attempts.Should().Be(4);
    }

    [Fact]
    public async Task Execute_AsynchronousPredicate_EnforcesBudget()
    {
        var pipeline = CreatePipeline(
            new() { RetryRatio = 0.5, MinimumThroughput = 1 },
            shouldHandle: async _ =>
            {
                await Task.Yield();
                return true;
            });

        using var response = await pipeline.ExecuteAsync(_ => FailAsync());

        _attempts.Should().Be(2);
    }

    [Fact]
    public async Task Execute_NestedPipelines_UseTheirOwnBudget()
    {
        var outer = CreatePipeline(new() { RetryRatio = 0 });
        var inner = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddHttpRetry(new HttpRetryStrategyOptions
            {
                Delay = TimeSpan.Zero,
                ShouldHandle = _ => new ValueTask<bool>(false),
                RetryBudget = new() { MinimumThroughput = 1000 },
            })
            .Build();

        var context = ResilienceContextPool.Shared.Get();
        try
        {
            using var response = await outer.ExecuteAsync(ctx => inner.ExecuteAsync(_ => FailAsync(), ctx), context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }

        // The outer budget (ratio 0) must be used for the outer retry decision, not the inner one.
        _attempts.Should().Be(1);
    }

    [Fact]
    public async Task Execute_BudgetIsSharedAcrossExecutions()
    {
        var pipeline = CreatePipeline(new() { RetryRatio = 0.5, MinimumThroughput = 4 }, maxRetryAttempts: 1);

        for (int i = 0; i < 3; i++)
        {
            using var response = await pipeline.ExecuteAsync(_ => FailAsync());
        }

        // Below the minimum throughput every execution retries once.
        _attempts.Should().Be(6);

        _attempts = 0;
        using var last = await pipeline.ExecuteAsync(_ => FailAsync());

        // 4 requests allow 2 retries, which are already used.
        _attempts.Should().Be(1);
    }

    private ValueTask<HttpResponseMessage> FailAsync()
    {
        _attempts++;

        // Ownership transfers to the pipeline, which disposes retried results.
#pragma warning disable CA2000 // Dispose objects before losing scope
        return new ValueTask<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.InternalServerError));
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    private ResiliencePipeline<HttpResponseMessage> CreatePipeline(
        HttpRetryBudgetOptions budget,
        int maxRetryAttempts = 3,
        Func<RetryPredicateArguments<HttpResponseMessage>, ValueTask<bool>>? shouldHandle = null)
    {
        var options = new HttpRetryStrategyOptions
        {
            Name = "my-retry",
            Delay = TimeSpan.Zero,
            MaxRetryAttempts = maxRetryAttempts,
            RetryBudget = budget,
        };

        if (shouldHandle is not null)
        {
            options.ShouldHandle = shouldHandle;
        }

        var telemetry = new TelemetryOptions();
        telemetry.TelemetryListeners.Add(new Listener(_events));

        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddHttpRetry(options)
            .ConfigureTelemetry(telemetry)
            .Build();
    }

    private sealed class Listener : TelemetryListener
    {
        private readonly List<TelemetryEventArguments<object, object>> _events;

        public Listener(List<TelemetryEventArguments<object, object>> events)
        {
            _events = events;
        }

        public override void Write<TResult, TArgs>(in TelemetryEventArguments<TResult, TArgs> args)
        {
            Outcome<object>? outcome = null;

            if (args.Outcome is { } value)
            {
                outcome = value.Exception is null ? Outcome.FromResult<object>(value.Result!) : Outcome.FromException<object>(value.Exception);
            }

            _events.Add(new TelemetryEventArguments<object, object>(args.Source, args.Event, args.Context, args.Arguments!, outcome));
        }
    }
}
