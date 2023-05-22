// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Extensions.Resilience.Test.Helpers;
using Microsoft.Shared.Text;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test;

public partial class ResiliencePipelineBuilderExtensionsTest
{
    [Fact]
    public void AddBulkheadPolicy_EnsureValidation()
    {
        _builder.AddBulkheadPolicy(PolicyName, options => options.MaxQueuedActions = -1);

        Assert.Throws<OptionsValidationException>(() => CreatePipeline());
    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddBulkheadPolicy_NullBuilder_Throws(MethodArgs mode)
    {
        IResiliencePipelineBuilder<string> builder = null!;

        Assert.Throws<ArgumentNullException>(() => AddBulkheadPolicy(mode, builder, "dummy", EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddBulkheadPolicy_NullOrEmptyPolicyName_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddBulkheadPolicy(mode, _builder, null!, EmptyConfiguration, options => { }));
        Assert.Throws<ArgumentException>(() => AddBulkheadPolicy(mode, _builder, string.Empty, EmptyConfiguration, options => { }));
    }

    [MemberData(nameof(ConfigureMethodCombinations))]
    [Theory]
    public void AddBulkheadPolicy_NullConfigureMethod_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddBulkheadPolicy(mode, _builder, "dummy", EmptyConfiguration, null!));

    }

    [MemberData(nameof(ConfigurationCombinations))]
    [Theory]
    public void AddBulkheadPolicy_NullConfiguration_Throws(MethodArgs mode)
    {
        Assert.Throws<ArgumentNullException>(() => AddBulkheadPolicy(mode, _builder, "dummy", null!, options => { }));

    }

    [MemberData(nameof(AllCombinations))]
    [Theory]
    public void AddBulkheadPolicy_Ok(MethodArgs mode)
    {
        // arrange
        var expectedQueuedActions = 13;
        var expectedConcurrency = 48;
        var configuration = CreateConfiguration("MaxQueuedActions", expectedQueuedActions.ToInvariantString());
        var optionsName = OptionsNameHelper.GetPolicyOptionsName(SupportedPolicies.BulkheadPolicy, DefaultPipelineName, PolicyName);

        AddBulkheadPolicy(mode, _builder, PolicyName, configuration, options => options.MaxConcurrency = expectedConcurrency);

        var serviceProvider = _builder.Services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IResiliencePipelineFactory>();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<BulkheadPolicyOptions>>().Get(optionsName);

        _pipelineBuilder.Setup(v => v.AddBulkheadPolicy(PolicyName, options)).Returns(_pipelineBuilder.Object).Verifiable();
        _pipelineBuilder.Setup(v => v.Build()).Returns(Policy.NoOpAsync<string>()).Verifiable();

        // act
        factory.CreatePipeline<string>(DefaultPipelineName, DefaultPipelineKey);

        // assert
        _pipelineBuilder.VerifyAll();

        AssertOptions(options, o => o.MaxQueuedActions, expectedQueuedActions, mode.HasFlag(MethodArgs.Configuration));
        AssertOptions(options, o => o.MaxConcurrency, expectedConcurrency, mode.HasFlag(MethodArgs.ConfigureMethod));
    }

    private static void AddBulkheadPolicy(
        MethodArgs mode,
        IResiliencePipelineBuilder<string> builder,
        string policyName,
        IConfigurationSection configuration,
        Action<BulkheadPolicyOptions> configureMethod)
    {
        _ = mode switch
        {
            MethodArgs.None => builder.AddBulkheadPolicy(policyName),
            MethodArgs.Configuration => builder.AddBulkheadPolicy(policyName, configuration),
            MethodArgs.ConfigureMethod => builder.AddBulkheadPolicy(policyName, configureMethod),
            MethodArgs.Configuration | MethodArgs.ConfigureMethod => builder.AddBulkheadPolicy(policyName, configuration, configureMethod),
            _ => throw new NotSupportedException()
        };
    }
}
