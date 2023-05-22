// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Extensions.Resilience.Test.Helpers;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;

public class ResiliencePipelineBuilderExtensionsTest : ResilienceTestHelper
{
    private readonly IResiliencePipelineBuilder<string> _builder;
    private readonly Mock<Resilience.Internal.IPolicyPipelineBuilder<string>> _pipelineBuilder = new(MockBehavior.Strict);

    public ResiliencePipelineBuilderExtensionsTest()
    {
        Services.TryAddSingleton(_pipelineBuilder.Object);

        _builder = Services.AddResiliencePipeline<string>(DefaultPipelineName);
        _pipelineBuilder.Setup(b => b.Initialize(PipelineId.Create<string>(DefaultPipelineName, DefaultPipelineKey)));
    }

    [Fact]
    public void AddPolicy_ArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => Resilience.Internal.ResiliencePipelineBuilderExtensions.AddPolicy<string>(null!, (_, _) => { }));
        Assert.Throws<ArgumentNullException>(() => Resilience.Internal.ResiliencePipelineBuilderExtensions.AddPolicy(_builder, null!));
    }

    [Fact]
    public void AddPolicy_WithOptions_Ok()
    {
        var called = false;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "bulkhead:MaxQueuedActions", "11" } })
            .Build()
            .GetSection("bulkhead");
        _builder.AddPolicy<string, BulkheadPolicyOptions, DummyValidator>(
            SupportedPolicies.BulkheadPolicy,
            "test",
            options => options.Configure(configuration, o => o.MaxConcurrency = 22),
            (builder, options) =>
            {
                Assert.Equal(22, options.MaxConcurrency);
                Assert.Equal(11, options.MaxQueuedActions);
                called = true;
                builder.AddBulkheadPolicy("test", options);
            });

        var provider = Services.BuildServiceProvider();
        var resilienceProvider = provider.GetRequiredService<IResiliencePipelineFactory>();
        var options = provider
            .GetRequiredService<IOptionsMonitor<BulkheadPolicyOptions>>()
            .Get(OptionsNameHelper.GetPolicyOptionsName(SupportedPolicies.BulkheadPolicy, DefaultPipelineName, "test"));
        _pipelineBuilder.Setup(v => v.AddBulkheadPolicy("test", options)).Returns(_pipelineBuilder.Object);
        _pipelineBuilder.Setup(v => v.Build()).Returns(Policy.NoOpAsync<string>);

        resilienceProvider.CreatePipeline<string>(DefaultPipelineName, DefaultPipelineKey);

        Assert.True(called);
    }

    [Fact]
    public void AddPolicy_WithOptions_EnsureValidated()
    {
        _builder.AddPolicy<string, BulkheadPolicyOptions, DummyValidator>(
            SupportedPolicies.BulkheadPolicy,
            "test",
            o => o.Configure(o => o.MaxConcurrency = -1),
            (builder, options) => builder.AddBulkheadPolicy("test", options));

        Assert.Throws<OptionsValidationException>(() => CreatePipeline());
    }

    private class DummyValidator : IValidateOptions<BulkheadPolicyOptions>
    {
        public ValidateOptionsResult Validate(string? name, BulkheadPolicyOptions options)
        {
            if (options.MaxConcurrency < 0)
            {
                return ValidateOptionsResult.Fail("ERROR");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
