// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Extensions.Resilience.Test.Helpers;
using Polly;
using Polly.Timeout;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test;

public partial class ResiliencePipelineBuilderExtensionsTest
{
    [Fact]
    public void AddTimeoutPolicy_EnsureValidation()
    {
        _builder.AddTimeoutPolicy(PolicyName, options => options.TimeoutInterval = TimeSpan.MinValue);

        Assert.Throws<OptionsValidationException>(() => CreatePipeline());
    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddTimeoutPolicy_NullBuilder_Throws(MethodArgs mode)
    {
        IResiliencePipelineBuilder<string> builder = null!;

        Assert.Throws<ArgumentNullException>(() => AddTimeoutPolicy(mode, builder, "dummy", EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddTimeoutPolicy_NullOrEmptyPolicyName_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddTimeoutPolicy(mode, _builder, null!, EmptyConfiguration, options => { }));
        Assert.Throws<ArgumentException>(() => AddTimeoutPolicy(mode, _builder, string.Empty, EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(ConfigureMethodCombinations))]
    [Theory]
    public void AddTimeoutPolicy_NullConfigureMethod_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddTimeoutPolicy(mode, _builder, "dummy", EmptyConfiguration, null!));

    }

    [MemberData(nameof(ConfigurationCombinations))]
    [Theory]
    public void AddTimeoutPolicy_NullConfiguration_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddTimeoutPolicy(mode, _builder, "dummy", null!, options => { }));

    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddTimeoutPolicy_Ok(MethodArgs mode)
    {
        // arrange
        var expectedTimeoutInterval = TimeSpan.FromMilliseconds(123);
        var expectedTimeoutStrategy = TimeoutStrategy.Pessimistic;
        var configuration = CreateConfiguration("TimeoutInterval", expectedTimeoutInterval.ToString());
        var optionsName = OptionsNameHelper.GetPolicyOptionsName(SupportedPolicies.TimeoutPolicy, DefaultPipelineName, PolicyName);

        AddTimeoutPolicy(mode, _builder, PolicyName, configuration, options => options.TimeoutStrategy = expectedTimeoutStrategy);

        var serviceProvider = _builder.Services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IResiliencePipelineFactory>();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<TimeoutPolicyOptions>>().Get(optionsName);

        _pipelineBuilder.Setup(v => v.AddTimeoutPolicy(PolicyName, options)).Returns(_pipelineBuilder.Object).Verifiable();
        _pipelineBuilder.Setup(v => v.Build()).Returns(Policy.NoOpAsync<string>()).Verifiable();

        // act
        factory.CreatePipeline<string>(DefaultPipelineName, DefaultPipelineKey);

        // assert
        _pipelineBuilder.VerifyAll();

        AssertOptions(options, o => o.TimeoutInterval, expectedTimeoutInterval, mode.HasFlag(MethodArgs.Configuration));
        AssertOptions(options, o => o.TimeoutStrategy, expectedTimeoutStrategy, mode.HasFlag(MethodArgs.ConfigureMethod));
    }

    private static void AddTimeoutPolicy(
        MethodArgs mode,
        IResiliencePipelineBuilder<string> builder,
        string policyName,
        IConfigurationSection configuration,
        Action<TimeoutPolicyOptions> configureMethod)
    {
        _ = mode switch
        {
            MethodArgs.None => builder.AddTimeoutPolicy(policyName),
            MethodArgs.Configuration => builder.AddTimeoutPolicy(policyName, configuration),
            MethodArgs.ConfigureMethod => builder.AddTimeoutPolicy(policyName, configureMethod),
            MethodArgs.Configuration | MethodArgs.ConfigureMethod => builder.AddTimeoutPolicy(policyName, configuration, configureMethod),
            _ => throw new NotSupportedException()
        };
    }
}
