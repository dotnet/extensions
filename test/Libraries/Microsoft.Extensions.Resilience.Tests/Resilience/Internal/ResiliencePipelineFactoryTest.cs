// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;

public class ResiliencePipelineFactoryTest
{
    private const string PipelineName = "default-pipeline";
    private const string PipelineKey = "default-key";
    private static readonly PipelineId _pipelineId = PipelineId.Create<string>(PipelineName, PipelineKey);

    private readonly ServiceCollection _services;
    private readonly IResiliencePipelineBuilder<string> _builder;
    private readonly Mock<Resilience.Internal.IPolicyPipelineBuilder<string>> _pipelineBuilder = new(MockBehavior.Strict);
    private readonly IAsyncPolicy<string> _defaultPolicy = Policy.NoOpAsync<string>();
    private readonly TimeoutPolicyOptions _defaultOptions = new();

    public ResiliencePipelineFactoryTest()
    {
        _services = new ServiceCollection();
        _ = _services.AddLogging();
        _services.TryAddSingleton(_pipelineBuilder.Object);
        _builder = _services.AddResiliencePipeline<string>(PipelineName);
    }

    [Fact]
    public void Create_NotConfigured_Throws()
    {
        Assert.Throws<OptionsValidationException>(() => CreateFactory(out _).CreatePipeline<string>("not-configured", string.Empty));
    }

    [Fact]
    public void Create_NullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CreateFactory(out _).CreatePipeline<string>(null!, string.Empty));
        Assert.Throws<ArgumentException>(() => CreateFactory(out _).CreatePipeline<string>(string.Empty, string.Empty));
    }

    [Fact]
    public void AddPolicy_EnsureCalled()
    {
        _pipelineBuilder.Setup(v => v.AddTimeoutPolicy("test", _defaultOptions)).Returns(_pipelineBuilder.Object).Verifiable();
        _pipelineBuilder.Setup(v => v.Initialize(_pipelineId));
        _pipelineBuilder.Setup(v => v.Build()).Returns(_defaultPolicy).Verifiable();

        AddPolicy(builder => builder.AddTimeoutPolicy("test", _defaultOptions));

        var factory = CreateFactory(out var provider);

        var dynamicPolicy = factory.CreatePipeline<string>(PipelineName, PipelineKey) as AsyncDynamicPipeline<string>;
        Assert.Equal(_defaultPolicy, dynamicPolicy!.CurrentValue);

        _pipelineBuilder.VerifyAll();
    }

    [Fact]
    public void AddPolicy_Multiple_EnsureCalledAndOrderPreserved()
    {
        var otherOptions = new TimeoutPolicyOptions();
        var order = 0;

        _pipelineBuilder.Setup(v => v.AddTimeoutPolicy("test1", _defaultOptions)).Returns(_pipelineBuilder.Object).Callback(() =>
        {
            Assert.Equal(0, order);
            order++;
        }).Verifiable();

        _pipelineBuilder.Setup(v => v.AddTimeoutPolicy("test2", otherOptions)).Returns(_pipelineBuilder.Object).Callback(() =>
        {
            Assert.Equal(1, order);
            order++;
        }).Verifiable();
        _pipelineBuilder.Setup(v => v.Initialize(_pipelineId));
        _pipelineBuilder.Setup(v => v.Build()).Returns(_defaultPolicy).Verifiable();

        AddPolicy(builder => builder.AddTimeoutPolicy("test1", _defaultOptions));
        AddPolicy(builder => builder.AddTimeoutPolicy("test2", otherOptions));

        var factory = CreateFactory(out var provider);

        factory.CreatePipeline<string>(PipelineName, PipelineKey);

        _pipelineBuilder.VerifyAll();
    }

    [Fact]
    public void Create_EnsureRecreated()
    {
        _pipelineBuilder.Setup(v => v.AddTimeoutPolicy("test", _defaultOptions)).Returns(_pipelineBuilder.Object).Verifiable();
        _pipelineBuilder.Setup(v => v.Initialize(_pipelineId));
        _pipelineBuilder.Setup(v => v.Build()).Returns(() => Policy.NoOpAsync<string>()).Verifiable();

        AddPolicy(builder => builder.AddTimeoutPolicy("test", _defaultOptions));

        var factory = CreateFactory(out var provider);

        Assert.NotEqual(factory.CreatePipeline<string>(PipelineName, PipelineKey), factory.CreatePipeline<string>(PipelineName, PipelineKey));
    }

    [Fact]
    public void Create_WithPipelineKey()
    {
        _pipelineBuilder.Setup(v => v.AddTimeoutPolicy("test", _defaultOptions)).Returns(_pipelineBuilder.Object).Verifiable();
        _pipelineBuilder.Setup(v => v.Initialize(It.Is<PipelineId>(v => v.PipelineKey == "key")));
        _pipelineBuilder.Setup(v => v.Build()).Returns(() => Policy.NoOpAsync<string>()).Verifiable();

        AddPolicy(builder => builder.AddTimeoutPolicy("test", _defaultOptions));

        var factory = CreateFactory(out var provider);

        Assert.NotNull(factory.CreatePipeline<string>(PipelineName, "key"));
    }

    [Fact]
    public void Create_MultiplePipelines_EnsureDistinctResults()
    {
        var options1 = new TimeoutPolicyOptions();
        var options2 = new TimeoutPolicyOptions();

        _pipelineBuilder.Setup(v => v.Initialize(_pipelineId));
        _pipelineBuilder.Setup(v => v.Initialize(It.Is<PipelineId>(v => v.PipelineName == "other" && v.PipelineKey == string.Empty)));
        _pipelineBuilder.Setup(v => v.AddTimeoutPolicy("test", options1)).Returns(_pipelineBuilder.Object).Verifiable();
        _pipelineBuilder.Setup(v => v.AddTimeoutPolicy("test", options2)).Returns(_pipelineBuilder.Object).Verifiable();
        _pipelineBuilder.Setup(v => v.Build()).Returns(() => Policy.NoOpAsync<string>()).Verifiable();

        AddPolicy(builder => builder.AddTimeoutPolicy("test", options1), PipelineName);
        AddPolicy(builder => builder.AddTimeoutPolicy("test", options2), "other");

        var factory = CreateFactory(out var provider);

        Assert.NotEqual(factory.CreatePipeline<string>(PipelineName, PipelineKey), factory.CreatePipeline<string>("other", string.Empty));

        _pipelineBuilder.VerifyAll();
    }

    private IResiliencePipelineFactory CreateFactory(out IServiceProvider serviceProvider)
    {
        serviceProvider = _services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<IResiliencePipelineFactory>();
    }

    private void AddPolicy(Action<Resilience.Internal.IPolicyPipelineBuilder<string>> configure, string pipelineName = PipelineName)
    {
        _builder.Services.AddResiliencePipeline<string>(pipelineName).AddPolicy((builder, _) => configure(builder));
    }
}
