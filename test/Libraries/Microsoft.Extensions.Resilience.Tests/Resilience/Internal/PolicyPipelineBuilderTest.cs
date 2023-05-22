// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Extensions.Resilience.Polly.Test.Hedging;
using Microsoft.Extensions.Resilience.Polly.Test.Options;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;

public sealed class PolicyPipelineBuilderTest : IDisposable
{
    private const string DefaultPolicyName = "default-policy-name-for-test";

    private static readonly FallbackPolicyOptions _defaultFallbackPolicyOptions = new();
    private static readonly CircuitBreakerPolicyOptions _defaultCircuitBreakerConfig = new();
    private static readonly RetryPolicyOptions _defaultRetryPolicyConfig = new();
    private static readonly HedgingPolicyOptions _defaultHedgingPolicyOptions = new();
    private static readonly IAsyncPolicy _defaultPolicy = Policy.NoOpAsync();
    private static readonly FallbackScenarioTaskProvider _defaultFallbackAction = _ => Task.FromResult("42");

    private readonly Mock<IPolicyFactory> _policyFactoryMock;
    private readonly Mock<IPipelineMetering> _metering;
    private readonly PolicyPipelineBuilder _builder;

    public PolicyPipelineBuilderTest()
    {
        _metering = new Mock<IPipelineMetering>(MockBehavior.Strict);
        _policyFactoryMock = new Mock<IPolicyFactory>(MockBehavior.Strict);
        _builder = new PolicyPipelineBuilder(_policyFactoryMock.Object, _metering.Object, NullLogger<PipelineTelemetry>.Instance);
    }

    public void Dispose()
    {
        _metering.VerifyAll();
        _policyFactoryMock.VerifyAll();
    }

    [Fact]
    public void AddCircuitBreakerPolicy_ValidConfiguration_ShouldReturnInstance()
    {
        _policyFactoryMock
            .Setup(mock => mock.CreateCircuitBreakerPolicy(DefaultPolicyName, _defaultCircuitBreakerConfig))
            .Returns(_defaultPolicy);

        var builderWithPolicy = _builder.AddCircuitBreakerPolicy(DefaultPolicyName, _defaultCircuitBreakerConfig);
        Assert.NotNull(builderWithPolicy);
    }

    [Fact]
    public void AddCircuitBreakerPolicy_NullArguments_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.AddCircuitBreakerPolicy(DefaultPolicyName, null!));
    }

    [Fact]
    public void AddRetryPolicy_ValidConfiguration_ShouldReturnInstance()
    {
        _policyFactoryMock
            .Setup(mock => mock.CreateRetryPolicy(DefaultPolicyName, _defaultRetryPolicyConfig))
            .Returns(_defaultPolicy);

        var builderWithPolicy = _builder.AddRetryPolicy(DefaultPolicyName, _defaultRetryPolicyConfig);
        Assert.NotNull(builderWithPolicy);
    }

    [Fact]
    public void AddRetryPolicy_NullConfiguration_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.AddRetryPolicy(DefaultPolicyName, null!));
    }

    [Fact]
    public void AddTimeoutPolicy_ValidConfiguration_ShouldReturnInstance()
    {
        var defaultTimeoutOptions = new TimeoutPolicyOptions
        {
            TimeoutInterval = TimeSpan.FromSeconds(10)
        };

        _policyFactoryMock
            .Setup(mock => mock.CreateTimeoutPolicy(DefaultPolicyName, defaultTimeoutOptions))
            .Returns(_defaultPolicy);

        var builderWithPolicy = _builder.AddTimeoutPolicy(DefaultPolicyName, defaultTimeoutOptions);
        Assert.NotNull(builderWithPolicy);
    }

    [Fact]
    public void AddTimeoutPolicy_MultipleTimeoutPolicies_ShouldAllowAndReturnInstance()
    {
        var primaryTimeoutOptions = new TimeoutPolicyOptions { TimeoutInterval = TimeSpan.FromSeconds(30) };
        var secondaryTimeoutOptions = new TimeoutPolicyOptions { TimeoutInterval = TimeSpan.FromSeconds(10) };
        _policyFactoryMock
            .Setup(mock => mock.CreateTimeoutPolicy("primary-timeout", primaryTimeoutOptions))
            .Returns(_defaultPolicy);
        _policyFactoryMock
            .Setup(mock => mock.CreateTimeoutPolicy("secondary-timeout", secondaryTimeoutOptions))
            .Returns(_defaultPolicy);

        var builderWithPolicy = _builder
            .AddTimeoutPolicy("primary-timeout", primaryTimeoutOptions)
            .AddTimeoutPolicy("secondary-timeout", secondaryTimeoutOptions);

        Assert.Equal(_builder, builderWithPolicy);
    }

    [Fact]
    public void AddFallbackPolicy_ValidConfiguration_ShouldReturnInstance()
    {
        _policyFactoryMock
            .Setup(mock => mock.CreateFallbackPolicy(
                DefaultPolicyName,
                It.IsAny<FallbackScenarioTaskProvider>(),
                _defaultFallbackPolicyOptions))
            .Returns(_defaultPolicy);

        var builderWithPolicy = _builder.AddFallbackPolicy(
            DefaultPolicyName,
            _defaultFallbackAction,
            _defaultFallbackPolicyOptions);

        Assert.NotNull(builderWithPolicy);
    }

    [Fact]
    public void AddFallbackPolicy_NullArguments_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.AddFallbackPolicy(DefaultPolicyName, null!, _defaultFallbackPolicyOptions));

        Assert.Throws<ArgumentNullException>(() => _builder.AddFallbackPolicy(DefaultPolicyName, _defaultFallbackAction, null!));
    }

    [Fact]
    public void AddBulkheadPolicy_Null_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _builder.AddBulkheadPolicy(DefaultPolicyName, null!));
    }

    [Fact]
    public void AddBulkheadPolicy_ShouldReturnInstance()
    {
        var options = Constants.BulkheadPolicy.DefaultOptions;

        _policyFactoryMock
            .Setup(mock => mock.CreateBulkheadPolicy(DefaultPolicyName, options))
            .Returns(_defaultPolicy);

        var policy = _builder.AddBulkheadPolicy(DefaultPolicyName, options);
        Assert.NotNull(policy);
    }

    [Fact]
    public void AddHedgingPolicy_ValidConfiguration_ShouldReturnInstance()
    {
        _policyFactoryMock
            .Setup(mock => mock.CreateHedgingPolicy(
                DefaultPolicyName,
                HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProviderNonGeneric,
                _defaultHedgingPolicyOptions))
            .Returns(_defaultPolicy);

        var builderWithPolicy = _builder.AddHedgingPolicy(
            DefaultPolicyName,
            HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProviderNonGeneric,
            _defaultHedgingPolicyOptions);

        Assert.NotNull(builderWithPolicy);
    }

    [Fact]
    public void AddHedgingPolicy_NullArguments_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(
            () => _builder.AddHedgingPolicy(
                DefaultPolicyName,
                HedgingTestUtilities<string>.HedgedTasksHandler.FunctionsProviderNonGeneric,
                null!));

        Assert.Throws<ArgumentNullException>(
            () => _builder.AddHedgingPolicy(
                DefaultPolicyName,
                null!,
                _defaultHedgingPolicyOptions));
    }

    [Fact]
    public void Build_SinglePolicyConfigured_ShouldReturnSamePolicy()
    {
        _policyFactoryMock
            .Setup(mock => mock.CreateCircuitBreakerPolicy(DefaultPolicyName, _defaultCircuitBreakerConfig))
            .Returns(_defaultPolicy);

        var singlePolicy = _builder.AddCircuitBreakerPolicy(DefaultPolicyName, _defaultCircuitBreakerConfig).Build();
        Assert.NotNull(singlePolicy);
        Assert.Equal(_defaultPolicy, singlePolicy);
    }

    [Fact]
    public void Build_MultiplePoliciesConfigured_ShouldReturnPolicyWrap()
    {
        _policyFactoryMock
            .Setup(mock => mock.CreateCircuitBreakerPolicy("circuit", _defaultCircuitBreakerConfig))
            .Returns(_defaultPolicy);
        _policyFactoryMock
            .Setup(mock => mock.CreateRetryPolicy("retry", _defaultRetryPolicyConfig))
            .Returns(_defaultPolicy);

        var policy = _builder
            .AddCircuitBreakerPolicy("circuit", _defaultCircuitBreakerConfig)
            .AddRetryPolicy("retry", _defaultRetryPolicyConfig)
            .Build();
        Assert.NotNull(policy);

        var policyWrap = policy as AsyncPolicyPipeline;
        Assert.NotNull(policyWrap);
    }

    [InlineData("name", "", "String-name")]
    [InlineData("name", "key", "String-name-key")]
    [Theory]
    public void SetPipelineIdentifiers_EnsurePolicyKeyAssignedAndMeteredPolicyReturned(string name, string key, string expectedKey)
    {
        var id = PipelineId.Create<string>(name, key);

        // arrange
        _policyFactoryMock
            .Setup(mock => mock.CreateCircuitBreakerPolicy("circuit", _defaultCircuitBreakerConfig))
            .Returns(() => Policy.NoOpAsync());
        _policyFactoryMock.Setup(f => f.Initialize(id));
        _metering.Setup(v => v.Initialize(id));

        // act
        _builder.Initialize(id);

        var policy = _builder
            .AddCircuitBreakerPolicy("circuit", _defaultCircuitBreakerConfig)
            .Build();

        // assert
        Assert.Equal(expectedKey, policy.PolicyKey);
        Assert.IsType<PipelineTelemetry.TelemetryPolicy>(policy);
    }

    [Fact]
    public void Build_NoPoliciesConfigured_ShouldThrow()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => _builder.Build());
        Assert.Equal("At least one policy must be configured.", exception.Message);
    }
}
