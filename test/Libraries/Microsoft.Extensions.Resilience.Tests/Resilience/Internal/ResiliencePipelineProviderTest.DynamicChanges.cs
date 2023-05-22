// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;

public sealed partial class ResiliencePipelineProviderTest : IDisposable
{
    private const string PipelineName = "testPipeline";
    private const string SecondaryPipelineName = "RandomPipeline";
    private const string PolicyName = "testPolicy";

    private const string DefaultConfigurationKey = "TimeoutPolicyOptions:TimeoutInterval";
    private const string OtherConfigurationKey = "TimeoutPolicyOptions_Other:TimeoutInterval";
    private const string NoNameConfigurationKey = "TimeoutPolicyOptions_NoName:TimeoutInterval";

    private static readonly string _updateFailureMessage = $"Pipeline update failed. Pipeline Name: {PipelineName}.";
    private static readonly string _updateSuccessMessage = $"Pipeline {PipelineName} has been updated.";

    public void Dispose()
    {
        _provider.Dispose();
        _factory.VerifyAll();
    }

    [Fact]
    public async Task GetPipeline_ConfigurationUpdatedForTargetPipeline_EnsureNewPolicy()
    {
        var config = new ReloadableConfiguration();
        var pipelineLogger = new FakeLogger<AsyncDynamicPipeline<string>>();
        var policyLogger = new FakeLogger<TimeoutPolicyOptions>();
        using var provider = GetPipelineProvider(pipelineLogger, policyLogger, config);

        var pipelineOnFirstCall = provider.GetPipeline<string>(PipelineName) as AsyncDynamicPipeline<string>;
        var pipelineOnSecondCall = provider.GetPipeline<string>(PipelineName) as AsyncDynamicPipeline<string>;
        Assert.NotNull(pipelineOnFirstCall);
        Assert.Same(pipelineOnFirstCall, pipelineOnSecondCall);
        Assert.Equal(0, pipelineLogger.Collector.Count);

        var pipelineOnFirstCallCurrent = pipelineOnFirstCall.CurrentValue;
        var pipelineOnSecondCallCurrent = pipelineOnSecondCall!.CurrentValue;
        Assert.Same(pipelineOnFirstCallCurrent, pipelineOnSecondCallCurrent);

        // Trigger onChange callback of the options monitor
        await config.UpdateTimeoutAndReloadAsync("00:15:00");

        var pipelineOnFirstCallAfterChange = provider.GetPipeline<string>(PipelineName) as AsyncDynamicPipeline<string>;
        Assert.NotNull(pipelineOnFirstCallAfterChange);
        Assert.Same(pipelineOnFirstCall, pipelineOnFirstCallAfterChange);

        var pipelineOnFirstCallAfterChangeCurrent = pipelineOnFirstCallAfterChange.CurrentValue;
        Assert.NotSame(pipelineOnFirstCallCurrent, pipelineOnFirstCallAfterChangeCurrent);

        // Just for my sanity, let's see if any change event got triggered in the background after multiple calls.
        await Task.Delay(5000);

        var pipelineOnSecondCallAfterChangeCurrent = GetCurrentValueOfPipeline(PipelineName, provider);
        Assert.Same(pipelineOnFirstCallAfterChangeCurrent, pipelineOnSecondCallAfterChangeCurrent);
        Assert.Equal(1, policyLogger.Collector.Count);
        Assert.Equal(1, pipelineLogger.Collector.Count);
        Assert.Equal(_updateSuccessMessage, pipelineLogger.LatestRecord.Message);
    }

    [Theory]
    [InlineData(OtherConfigurationKey)]
    [InlineData(NoNameConfigurationKey)]
    public async Task GetPipeline_ConfigurationUpdatedForDifferentConfigurationProvider_EnsureSamePolicy(
        string configurationKey)
    {
        var pipelineLogger = new FakeLogger<AsyncDynamicPipeline<string>>();
        var policyLogger = new FakeLogger<TimeoutPolicyOptions>();
        var configProvider1 = new ReloadableConfiguration();
        var configProvider2 = new ReloadableConfiguration();
        var config1 = new ConfigurationBuilder().Add(configProvider1).Build();
        var config2 = new ConfigurationBuilder().Add(configProvider2).Build();

        var services = GetServiceCollection(pipelineLogger, policyLogger);
        _ = services
           .AddResiliencePipeline<string>(PipelineName)
           .AddTimeoutPolicy(PolicyName, config1.GetSection("TimeoutPolicyOptions"));
        _ = services
            .AddResiliencePipeline<string>(SecondaryPipelineName)
            .AddTimeoutPolicy("otherPolicy", config2.GetSection("TimeoutPolicyOptions_Other"));
        _ = services.Configure<TimeoutPolicyOptions>(null, config2.GetSection("TimeoutPolicyOptions_NoName"));

        using var provider = GetPipelineProvider(services);
        var pipeline1 = GetCurrentValueOfPipeline(PipelineName, provider);
        var pipeline2 = GetCurrentValueOfPipeline(SecondaryPipelineName, provider);

        Assert.Equal(0, pipelineLogger.Collector.Count);
        Assert.Equal(0, policyLogger.Collector.Count);

        await configProvider2.UpdateTimeoutAndReloadAsync("00:15:00", configurationKey);

        // The first pipeline is not affected by the change
        var pipeline1AfterChange = GetCurrentValueOfPipeline(PipelineName, provider);
        Assert.Same(pipeline1, pipeline1AfterChange);

        // The second pipeline should be impacted by the change
        var pipeline2AfterChange = GetCurrentValueOfPipeline(SecondaryPipelineName, provider);
        Assert.NotSame(pipeline2, pipeline2AfterChange);

        Assert.Equal(1, policyLogger.Collector.Count);
        Assert.Equal(1, pipelineLogger.Collector.Count);
        Assert.Equal($"Pipeline {SecondaryPipelineName} has been updated.", pipelineLogger.LatestRecord.Message);
    }

    [Fact]
    public void GetPipeline_WhenNoChanges_RegistersOnlyOneListenerPerPipelineAndKey()
    {
        var onChangeCalls = 0;
        var optionsMonitorMock = new Mock<IOptionsMonitor<ResiliencePipelineFactoryOptions<string>>>(MockBehavior.Strict);
        var listenerMock = new Mock<IDisposable>(MockBehavior.Strict);
        var services = new ServiceCollection().RegisterMetering().AddLogging();

        var options = new ResiliencePipelineFactoryOptions<string>();
        var timeoutOptions = new TimeoutPolicyOptions();
        options.BuilderActions.Add(b => b.AddTimeoutPolicy(PolicyName, timeoutOptions));

        listenerMock.Setup(mock => mock.Dispose());
        optionsMonitorMock.Setup(mock => mock.Get(PipelineName)).Returns(options);
        optionsMonitorMock
            .SetupSequence(mock => mock.OnChange(It.IsAny<Action<ResiliencePipelineFactoryOptions<string>, string?>>()))
            .Returns(() =>
            {
                onChangeCalls++;
                return listenerMock.Object;
            })
            .Returns(() =>
            {
                onChangeCalls++;
                return listenerMock.Object;
            })
            .Returns(() =>
            {
                onChangeCalls++;
                return null!;
            });

        _ = services.AddResiliencePipeline<string>(PipelineName).AddTimeoutPolicy(PolicyName, o => o.TimeoutInterval = timeoutOptions.TimeoutInterval);
        _ = services.AddSingleton(optionsMonitorMock.Object);

        using var provider = GetPipelineProvider(services);

        var pipelineOnFirstCall = provider.GetPipeline<string>(PipelineName);
        var pipelineOnSecondCall = provider.GetPipeline<string>(PipelineName);
        Assert.Same(pipelineOnFirstCall, pipelineOnSecondCall);
        Assert.Equal(1, onChangeCalls);

        var key1 = "key1";
        var key2 = "key2";
        var pipelineWithKey1 = provider.GetPipeline<string>(PipelineName, key1);
        var pipelineWithKey2 = provider.GetPipeline<string>(PipelineName, key2);
        Assert.NotSame(pipelineWithKey1, pipelineWithKey2);
        Assert.Equal(3, onChangeCalls);
    }

    [Fact]
    public async Task GetPipeline_InvalidConfigurationUpdatedForTargetPipeline_ShouldUseSamePolicy()
    {
        var config = new ReloadableConfiguration();
        var pipelineLogger = new FakeLogger<AsyncDynamicPipeline<string>>();
        var policyLogger = new FakeLogger<TimeoutPolicyOptions>();
        using var provider = GetPipelineProvider(pipelineLogger, policyLogger, config);

        var pipelineCurrent = GetCurrentValueOfPipeline(PipelineName, provider);
        Assert.Equal(0, pipelineLogger.Collector.Count);
        Assert.Equal(0, policyLogger.Collector.Count);

        // Trigger onChange callback of the options monitor.
        await config.UpdateTimeoutAndReloadAsync("-00:15:00");

        var pipelineCurrentAfterChange = GetCurrentValueOfPipeline(PipelineName, provider);
        Assert.Same(pipelineCurrent, pipelineCurrentAfterChange);
        Assert.Equal(0, policyLogger.Collector.Count);
        Assert.Equal(1, pipelineLogger.Collector.Count);
        Assert.Equal(_updateFailureMessage, pipelineLogger.LatestRecord.Message);
    }

    [Fact]
    public async Task GetPipeline_MultipleOptionsAndOneInvalid_ShouldUseSamePolicy()
    {
        var configProvider = new ReloadableConfiguration();
        var config = new ConfigurationBuilder().Add(configProvider).Build();
        var pipelineLogger = new FakeLogger<AsyncDynamicPipeline<string>>();
        var timeoutPolicyLogger = new FakeLogger<TimeoutPolicyOptions>();
        var retryPolicyLogger = new FakeLogger<RetryPolicyOptions<string>>();

        var services = GetServiceCollection(pipelineLogger, timeoutPolicyLogger);
        _ = services
           .AddSingleton<ILogger<RetryPolicyOptions<string>>>(retryPolicyLogger)
           .AddResiliencePipeline<string>(PipelineName)
           .AddTimeoutPolicy(PolicyName, config.GetSection("TimeoutPolicyOptions"))
           .AddRetryPolicy(PolicyName, config.GetSection("RetryPolicyOptions"))
           .AddTimeoutPolicy("AnotherTimeout", config.GetSection("TimeoutPolicyOptions_Other"))
           .AddTimeoutPolicy("AnotherAnotherTimeout", config.GetSection("TimeoutPolicyOptions_NoName"));

        using var provider = GetPipelineProvider(services);
        var pipelineCurrent = GetCurrentValueOfPipeline(PipelineName, provider);
        Assert.Equal(0, pipelineLogger.Collector.Count);
        Assert.Equal(0, timeoutPolicyLogger.Collector.Count);

        // Trigger onChange callback of the options monitor
        await configProvider.UpdateTimeoutAndReloadAsync("-00:15:00");

        var pipelineCurrentAfterChange = GetCurrentValueOfPipeline(PipelineName, provider);
        Assert.Same(pipelineCurrent, pipelineCurrentAfterChange);

        // The update event will be triggered for each policy 1xRetry, 3xTimeout
        // but the OnChange event will not be propagated for the invalid value
        Assert.Equal(1, retryPolicyLogger.Collector.Count);
        Assert.Equal(2, timeoutPolicyLogger.Collector.Count);
        Assert.Equal(1, pipelineLogger.Collector.Count);
        Assert.Equal(_updateFailureMessage, pipelineLogger.LatestRecord.Message);
    }

    [Fact]
    public async Task GetPipeline_OnStaticActionConfigurations_EnsureNoUpdateImpactsPipelines()
    {
        var configProvider = new ReloadableConfiguration();
        var pipelineLogger = new FakeLogger<AsyncDynamicPipeline<string>>();
        var ignoredOptionsLogger = new FakeLogger<TimeoutPolicyOptions>();
        var policyLogger = new FakeLogger<CircuitBreakerPolicyOptions>();
        var config = new ConfigurationBuilder().Add(configProvider).Build();
        var services = GetServiceCollection(pipelineLogger, policyLogger);
        _ = services
           .Configure<TimeoutPolicyOptions>(config.GetSection("TimeoutPolicyOptions"))
           .AddSingleton<ILogger<TimeoutPolicyOptions>>(ignoredOptionsLogger)
           .AddResiliencePipeline<string>(PipelineName)
           .AddCircuitBreakerPolicy(PolicyName, o => o.FailureThreshold = 0.3);

        using var provider = GetPipelineProvider(services);
        var pipelineCurrent = GetCurrentValueOfPipeline(PipelineName, provider);

        await configProvider.UpdateTimeoutAndReloadAsync("00:15:00");

        var pipelineAfterUpdateCurrent = GetCurrentValueOfPipeline(PipelineName, provider);
        Assert.Same(pipelineCurrent, pipelineAfterUpdateCurrent);

        Assert.Equal(0, ignoredOptionsLogger.Collector.Count);
        Assert.Equal(0, policyLogger.Collector.Count);
        Assert.Equal(0, pipelineLogger.Collector.Count);
    }

    [Fact]
    public async Task GetPipeline_DuplicatedOptionsName_ShouldRegisterSingleListener()
    {
        var configProvider = new ReloadableConfiguration();
        var config = new ConfigurationBuilder().Add(configProvider).Build();
        var pipelineLogger = new FakeLogger<AsyncDynamicPipeline<string>>();
        var timeoutPolicyLogger = new FakeLogger<TimeoutPolicyOptions>();

        var services = GetServiceCollection(pipelineLogger, timeoutPolicyLogger);
        _ = services
           .AddResiliencePipeline<string>(PipelineName)
           .AddTimeoutPolicy(PolicyName, config.GetSection("TimeoutPolicyOptions"))
           .AddTimeoutPolicy(PolicyName, config.GetSection("TimeoutPolicyOptions"));

        using var provider = GetPipelineProvider(services);
        var pipelineCurrent = GetCurrentValueOfPipeline(PipelineName, provider);
        Assert.Equal(0, pipelineLogger.Collector.Count);
        Assert.Equal(0, timeoutPolicyLogger.Collector.Count);

        // Trigger onChange callback of the options monitor
        await configProvider.UpdateTimeoutAndReloadAsync("00:15:00");

        var pipelineCurrentAfterChange = GetCurrentValueOfPipeline(PipelineName, provider);
        Assert.NotSame(pipelineCurrent, pipelineCurrentAfterChange);

        Assert.Equal(2, timeoutPolicyLogger.Collector.Count);
        Assert.Equal(1, pipelineLogger.Collector.Count);
        Assert.Equal(_updateSuccessMessage, pipelineLogger.LatestRecord.Message);
    }

    private static ResiliencePipelineProvider GetPipelineProvider(
        ILogger<AsyncDynamicPipeline<string>> pipelineLogger,
        ILogger<TimeoutPolicyOptions> policyLogger,
        ReloadableConfiguration configProvider)
    {
        var config = new ConfigurationBuilder().Add(configProvider).Build();
        var services = GetServiceCollection(pipelineLogger, policyLogger);

        _ = services
           .AddResiliencePipeline<string>(PipelineName)
           .AddTimeoutPolicy(PolicyName, config.GetSection("TimeoutPolicyOptions"));

        return GetPipelineProvider(services);
    }

    private static ResiliencePipelineProvider GetPipelineProvider(IServiceCollection services)
    {
        return (services.BuildServiceProvider().GetRequiredService<IResiliencePipelineProvider>() as ResiliencePipelineProvider)!;
    }

    private static IAsyncPolicy<string> GetCurrentValueOfPipeline(string pipelineName, ResiliencePipelineProvider provider)
    {
        var pipeline = provider.GetPipeline<string>(pipelineName) as AsyncDynamicPipeline<string>;
        var current = pipeline?.CurrentValue;
        Assert.NotNull(current);

        return current;
    }

    private static IServiceCollection GetServiceCollection<TPolicyOptions>(
        ILogger<AsyncDynamicPipeline<string>> pipelineLogger,
        ILogger<TPolicyOptions> policyLogger)
    {
        return new ServiceCollection()
            .RegisterMetering()
            .AddLogging()
            .AddSingleton(pipelineLogger)
            .AddSingleton(policyLogger);
    }

    private class ReloadableConfiguration : ConfigurationProvider, IConfigurationSource
    {
        public ReloadableConfiguration()
        {
            Data = new Dictionary<string, string?>
            {
                { DefaultConfigurationKey, "00:00:10" },
                { OtherConfigurationKey, "00:00:11" },
                { NoNameConfigurationKey, "00:00:12" },
                { "RetryPolicyOptions:RetryCount", "4" }
            };
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return this;
        }

        public void Reload(Dictionary<string, string?> data)
        {
            foreach (var kvp in data)
            {
                Data[kvp.Key] = kvp.Value;
            }

            // Note: The event should run in a background thread.
            // This is faking that.
            _ = Task.Run(() => OnReload());
        }

        public async Task UpdateTimeoutAndReloadAsync(string timeoutValue, string key = DefaultConfigurationKey)
        {
            var content = new Dictionary<string, string?>(Data)
            {
                [key] = timeoutValue
            };

            Reload(content);
            await Task.Delay(5000);
        }
    }
}
