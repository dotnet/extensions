// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
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
    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddFallbackPolicy_NullBuilder_Throws(MethodArgs mode)
    {
        IResiliencePipelineBuilder<string> builder = null!;

        Assert.Throws<ArgumentNullException>(() => AddFallbackPolicy(mode, builder, "dummy", FallbackTask, EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddFallbackPolicy_NullOrEmptyPolicyName_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddFallbackPolicy(mode, _builder, null!, FallbackTask, EmptyConfiguration, options => { }));
        Assert.Throws<ArgumentException>(() => AddFallbackPolicy(mode, _builder, string.Empty, FallbackTask, EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(ConfigureMethodCombinations))]
    [Theory]
    public void AddFallbackPolicy_NullConfigureMethod_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddFallbackPolicy(mode, _builder, "dummy", FallbackTask, EmptyConfiguration, null!));

    }

    [MemberData(nameof(ConfigurationCombinations))]
    [Theory]
    public void AddFallbackPolicy_NullConfiguration_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddFallbackPolicy(mode, _builder, "dummy", FallbackTask, null!, options => { }));

    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddFallbackPolicy_NullFallbackScenarioTask_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddFallbackPolicy(mode, _builder, "dummy", null!, EmptyConfiguration, o => { }));

    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddFallbackPolicy_Ok(MethodArgs mode)
    {
        // arrange
        var expectedOnFallback = OnFallback;
        var configuration = CreateEmptyConfiguration();
        var optionsName = OptionsNameHelper.GetPolicyOptionsName(SupportedPolicies.FallbackPolicy, DefaultPipelineName, PolicyName);

        AddFallbackPolicy(mode, _builder, PolicyName, FallbackTask, configuration, options => options.OnFallbackAsync = expectedOnFallback);

        var serviceProvider = _builder.Services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IResiliencePipelineFactory>();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<FallbackPolicyOptions<string>>>().Get(optionsName);

        _pipelineBuilder.Setup(v => v.AddFallbackPolicy(PolicyName, FallbackTask, options)).Returns(_pipelineBuilder.Object).Verifiable();
        _pipelineBuilder.Setup(v => v.Build()).Returns(Policy.NoOpAsync<string>()).Verifiable();

        // act
        factory.CreatePipeline<string>(DefaultPipelineName, DefaultPipelineKey);

        // assert
        _pipelineBuilder.VerifyAll();

        AssertOptions(options, o => o.OnFallbackAsync, expectedOnFallback, mode.HasFlag(MethodArgs.ConfigureMethod));

        static Task<string> OnFallback(FallbackTaskArguments<string> args) => Task.FromResult(string.Empty);
    }

    private static void AddFallbackPolicy(
        MethodArgs mode,
        IResiliencePipelineBuilder<string> builder,
        string policyName,
        FallbackScenarioTaskProvider<string> provider,
        IConfigurationSection configuration,
        Action<FallbackPolicyOptions<string>> configureMethod)
    {
        _ = mode switch
        {
            MethodArgs.None => builder.AddFallbackPolicy(policyName, provider),
            MethodArgs.Configuration => builder.AddFallbackPolicy(policyName, provider, configuration),
            MethodArgs.ConfigureMethod => builder.AddFallbackPolicy(policyName, provider, configureMethod),
            MethodArgs.Configuration | MethodArgs.ConfigureMethod => builder.AddFallbackPolicy(policyName, provider, configuration, configureMethod),
            _ => throw new NotSupportedException()
        };
    }

    private static Task<string> FallbackTask(FallbackScenarioTaskArguments args) => Task.FromResult(string.Empty);
}
