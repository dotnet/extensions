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
using Xunit;

namespace Microsoft.Extensions.Resilience.Test;

public partial class ResiliencePipelineBuilderExtensionsTest
{
    [Fact]
    public void AddRetryPolicy_EnsureValidation()
    {
        _builder.AddRetryPolicy(PolicyName, options => options.RetryCount = -2);

        Assert.Throws<OptionsValidationException>(() => CreatePipeline());
    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddRetryPolicy_NullBuilder_Throws(MethodArgs mode)
    {
        IResiliencePipelineBuilder<string> builder = null!;

        Assert.Throws<ArgumentNullException>(() => AddRetryPolicy(mode, builder, "dummy", EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddRetryPolicy_NullOrEmptyPolicyName_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddRetryPolicy(mode, _builder, null!, EmptyConfiguration, options => { }));
        Assert.Throws<ArgumentException>(() => AddRetryPolicy(mode, _builder, string.Empty, EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(ConfigureMethodCombinations))]
    [Theory]
    public void AddRetryPolicy_NullConfigureMethod_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddRetryPolicy(mode, _builder, "dummy", EmptyConfiguration, null!));

    }

    [MemberData(nameof(ConfigurationCombinations))]
    [Theory]
    public void AddRetryPolicy_NullConfiguration_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddRetryPolicy(mode, _builder, "dummy", null!, options => { }));

    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddRetryPolicy_Ok(MethodArgs mode)
    {
        // arrange
        var expectedBaseDelay = TimeSpan.FromMilliseconds(123);
        var expectedRetryCount = 8;
        var configuration = CreateConfiguration("BaseDelay", expectedBaseDelay.ToString());
        var optionsName = OptionsNameHelper.GetPolicyOptionsName(SupportedPolicies.RetryPolicy, DefaultPipelineName, PolicyName);

        AddRetryPolicy(mode, _builder, PolicyName, configuration, options => options.RetryCount = expectedRetryCount);

        var serviceProvider = _builder.Services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IResiliencePipelineFactory>();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<RetryPolicyOptions<string>>>().Get(optionsName);

        _pipelineBuilder.Setup(v => v.AddRetryPolicy(PolicyName, options)).Returns(_pipelineBuilder.Object).Verifiable();
        _pipelineBuilder.Setup(v => v.Build()).Returns(Policy.NoOpAsync<string>()).Verifiable();

        // act
        factory.CreatePipeline<string>(DefaultPipelineName, DefaultPipelineKey);

        // assert
        _pipelineBuilder.VerifyAll();

        AssertOptions(options, o => o.BaseDelay, expectedBaseDelay, mode.HasFlag(MethodArgs.Configuration));
        AssertOptions(options, o => o.RetryCount, expectedRetryCount, mode.HasFlag(MethodArgs.ConfigureMethod));
    }

    private static void AddRetryPolicy(
        MethodArgs mode,
        IResiliencePipelineBuilder<string> builder,
        string policyName,
        IConfigurationSection configuration,
        Action<RetryPolicyOptions<string>> configureMethod)
    {
        _ = mode switch
        {
            MethodArgs.None => builder.AddRetryPolicy(policyName),
            MethodArgs.Configuration => builder.AddRetryPolicy(policyName, configuration),
            MethodArgs.ConfigureMethod => builder.AddRetryPolicy(policyName, configureMethod),
            MethodArgs.Configuration | MethodArgs.ConfigureMethod => builder.AddRetryPolicy(policyName, configuration, configureMethod),
            _ => throw new NotSupportedException()
        };
    }
}
