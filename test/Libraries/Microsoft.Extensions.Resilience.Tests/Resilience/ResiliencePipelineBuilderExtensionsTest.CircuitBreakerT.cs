// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Extensions.Resilience.Test.Helpers;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test;

public partial class ResiliencePipelineBuilderExtensionsTest
{
    [Fact]
    public void AddCircuitBreakerPolicy_EnsureValidation()
    {
        _builder.AddCircuitBreakerPolicy(PolicyName, options => options.BreakDuration = TimeSpan.MinValue);

        Assert.Throws<OptionsValidationException>(() => CreatePipeline());
    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddCircuitBreakerPolicy_NullBuilder_Throws(MethodArgs mode)
    {
        IResiliencePipelineBuilder<string> builder = null!;

        Assert.Throws<ArgumentNullException>(() => AddCircuitBreakerPolicy(mode, builder, "dummy", EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddCircuitBreakerPolicy_NullOrEmptyPolicyName_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddCircuitBreakerPolicy(mode, _builder, null!, EmptyConfiguration, options => { }));
        Assert.Throws<ArgumentException>(() => AddCircuitBreakerPolicy(mode, _builder, string.Empty, EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(ConfigureMethodCombinations))]
    [Theory]
    public void AddCircuitBreakerPolicy_NullConfigureMethod_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddCircuitBreakerPolicy(mode, _builder, "dummy", EmptyConfiguration, null!));

    }

    [MemberData(nameof(ConfigurationCombinations))]
    [Theory]
    public void AddCircuitBreakerPolicy_NullConfiguration_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddCircuitBreakerPolicy(mode, _builder, "dummy", null!, options => { }));

    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddCircuitBreakerPolicy_Ok(MethodArgs mode)
    {
        // arrange
        var expectedFailureThreshold = 0.001;
        var expectedMinimumThroughput = 8;
        var configuration = CreateConfiguration("FailureThreshold", expectedFailureThreshold.ToString(CultureInfo.InvariantCulture));
        var optionsName = OptionsNameHelper.GetPolicyOptionsName(SupportedPolicies.CircuitBreaker, DefaultPipelineName, PolicyName);

        AddCircuitBreakerPolicy(mode, _builder, PolicyName, configuration, options => options.MinimumThroughput = expectedMinimumThroughput);

        var serviceProvider = _builder.Services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IResiliencePipelineFactory>();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<CircuitBreakerPolicyOptions<string>>>().Get(optionsName);

        _pipelineBuilder.Setup(v => v.AddCircuitBreakerPolicy(PolicyName, options)).Returns(_pipelineBuilder.Object).Verifiable();
        _pipelineBuilder.Setup(v => v.Build()).Returns(Policy.NoOpAsync<string>()).Verifiable();

        // act
        factory.CreatePipeline<string>(DefaultPipelineName, DefaultPipelineKey);

        // assert
        _pipelineBuilder.VerifyAll();

        AssertOptions(options, o => o.FailureThreshold, expectedFailureThreshold, mode.HasFlag(MethodArgs.Configuration));
        AssertOptions(options, o => o.MinimumThroughput, expectedMinimumThroughput, mode.HasFlag(MethodArgs.ConfigureMethod));
    }

    private static void AddCircuitBreakerPolicy(
        MethodArgs mode,
        IResiliencePipelineBuilder<string> builder,
        string policyName,
        IConfigurationSection configuration,
        Action<CircuitBreakerPolicyOptions<string>> configureMethod)
    {
        _ = mode switch
        {
            MethodArgs.None => builder.AddCircuitBreakerPolicy(policyName),
            MethodArgs.Configuration => builder.AddCircuitBreakerPolicy(policyName, configuration),
            MethodArgs.ConfigureMethod => builder.AddCircuitBreakerPolicy(policyName, configureMethod),
            MethodArgs.Configuration | MethodArgs.ConfigureMethod => builder.AddCircuitBreakerPolicy(policyName, configuration, configureMethod),
            _ => throw new NotSupportedException()
        };
    }
}
