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
    [Fact]
    public void AddHedgingPolicy_EnsureValidation()
    {
        _builder.AddHedgingPolicy(PolicyName, HedgedTaskProvider, options => options.MaxHedgedAttempts = -1);

        Assert.Throws<OptionsValidationException>(() => CreatePipeline());
    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddHedgingPolicy_NullBuilder_Throws(MethodArgs mode)
    {
        IResiliencePipelineBuilder<string> builder = null!;

        Assert.Throws<ArgumentNullException>(() => AddHedgingPolicy(mode, builder, "dummy", HedgedTaskProvider, EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddHedgingPolicy_NullOrEmptyPolicyName_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddHedgingPolicy(mode, _builder, null!, HedgedTaskProvider, EmptyConfiguration, options => { }));
        Assert.Throws<ArgumentException>(() => AddHedgingPolicy(mode, _builder, string.Empty, HedgedTaskProvider, EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(ConfigureMethodCombinations))]
    [Theory]
    public void AddHedgingPolicy_NullConfigureMethod_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddHedgingPolicy(mode, _builder, "dummy", HedgedTaskProvider, EmptyConfiguration, null!));

    }

    [MemberData(nameof(ConfigurationCombinations))]
    [Theory]
    public void AddHedgingPolicy_NullConfiguration_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddHedgingPolicy(mode, _builder, "dummy", HedgedTaskProvider, null!, options => { }));

    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddHedgingPolicy_NullHedgedTaskProvider_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddHedgingPolicy(mode, _builder, "dummy", null!, EmptyConfiguration, o => { }));

    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddHedgingPolicy_Ok(MethodArgs mode)
    {
        // arrange
        var expectedHedgingDelay = TimeSpan.FromMilliseconds(123);
        var expectedMaxHedgedAttempts = 8;
        var configuration = CreateConfiguration("HedgingDelay", expectedHedgingDelay.ToString());
        var optionsName = OptionsNameHelper.GetPolicyOptionsName(SupportedPolicies.HedgingPolicy, DefaultPipelineName, PolicyName);

        AddHedgingPolicy(mode, _builder, PolicyName, HedgedTaskProvider, configuration, options => options.MaxHedgedAttempts = expectedMaxHedgedAttempts);

        var serviceProvider = _builder.Services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IResiliencePipelineFactory>();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<HedgingPolicyOptions<string>>>().Get(optionsName);

        _pipelineBuilder.Setup(v => v.AddHedgingPolicy(PolicyName, HedgedTaskProvider, options)).Returns(_pipelineBuilder.Object).Verifiable();
        _pipelineBuilder.Setup(v => v.Build()).Returns(Policy.NoOpAsync<string>()).Verifiable();

        // act
        factory.CreatePipeline<string>(DefaultPipelineName, DefaultPipelineKey);

        // assert
        _pipelineBuilder.VerifyAll();

        AssertOptions(options, o => o.HedgingDelay, expectedHedgingDelay, mode.HasFlag(MethodArgs.Configuration));
        AssertOptions(options, o => o.MaxHedgedAttempts, expectedMaxHedgedAttempts, mode.HasFlag(MethodArgs.ConfigureMethod));
    }

    private static void AddHedgingPolicy(
        MethodArgs mode,
        IResiliencePipelineBuilder<string> builder,
        string policyName,
        HedgedTaskProvider<string> provider,
        IConfigurationSection configuration,
        Action<HedgingPolicyOptions<string>> configureMethod)
    {
        _ = mode switch
        {
            MethodArgs.None => builder.AddHedgingPolicy(policyName, provider),
            MethodArgs.Configuration => builder.AddHedgingPolicy(policyName, provider, configuration),
            MethodArgs.ConfigureMethod => builder.AddHedgingPolicy(policyName, provider, configureMethod),
            MethodArgs.Configuration | MethodArgs.ConfigureMethod => builder.AddHedgingPolicy(policyName, provider, configuration, configureMethod),
            _ => throw new NotSupportedException()
        };
    }

    private static bool HedgedTaskProvider(HedgingTaskProviderArguments args, out Task<string>? task)
    {
        task = Task.FromResult(string.Empty);
        return true;
    }
}
