// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Resilience.Internal;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;

public sealed partial class ResiliencePipelineProviderTest
{
    private readonly Mock<IResiliencePipelineFactory> _factory = new(MockBehavior.Strict);
    private readonly ResiliencePipelineProvider _provider;

    public ResiliencePipelineProviderTest()
    {
        _provider = new ResiliencePipelineProvider(_factory.Object);
    }

    [Fact]
    public void GetPipeline_ArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.GetPipeline<string>(null!));
        Assert.Throws<ArgumentException>(() => _provider.GetPipeline<string>(string.Empty));

        Assert.Throws<ArgumentNullException>(() => _provider.GetPipeline<string>(null!, "key"));
        Assert.Throws<ArgumentException>(() => _provider.GetPipeline<string>(string.Empty, "key"));

        Assert.Throws<ArgumentException>(() => _provider.GetPipeline<string>(string.Empty));

        Assert.Throws<ArgumentNullException>(() => _provider.GetPipeline<string>("name", null!));
        Assert.Throws<ArgumentException>(() => _provider.GetPipeline<string>("name", string.Empty));
    }

    [Fact]
    public void GetPipeline_EnsureCached()
    {
        _factory.Setup(v => v.CreatePipeline<string>("test", string.Empty)).Returns(() => Policy.NoOpAsync<string>()).Verifiable();

        Assert.Same(_provider.GetPipeline<string>("test"), _provider.GetPipeline<string>("test"));

        _factory.VerifyAll();
    }

    [Fact]
    public void GetPipeline_ByDifferentKeys_EnsureNotSame()
    {
        var calls = 0;

        _factory.Setup(v => v.CreatePipeline<string>("test", "A")).Callback(() => calls++).Returns(() => Policy.NoOpAsync<string>()).Verifiable();
        _factory.Setup(v => v.CreatePipeline<string>("test", "B")).Callback(() => calls++).Returns(() => Policy.NoOpAsync<string>()).Verifiable();

        Assert.NotSame(_provider.GetPipeline<string>("test", "A"), _provider.GetPipeline<string>("test", "B"));
        Assert.Same(_provider.GetPipeline<string>("test", "A"), _provider.GetPipeline<string>("test", "A"));
        Assert.Same(_provider.GetPipeline<string>("test", "B"), _provider.GetPipeline<string>("test", "B"));

        Assert.Equal(2, calls);

        _factory.VerifyAll();
    }

    [Fact]
    public void GetPipeline_SamePolicyNameDifferentTypes_EnsureDistinctPolicyInstances()
    {
        _factory.Setup(v => v.CreatePipeline<string>("test", string.Empty)).Returns(() => Policy.NoOpAsync<string>()).Verifiable();
        _factory.Setup(v => v.CreatePipeline<int>("test", string.Empty)).Returns(() => Policy.NoOpAsync<int>()).Verifiable();

        Assert.NotSame(_provider.GetPipeline<string>("test"), _provider.GetPipeline<int>("test"));

        _factory.VerifyAll();
    }
}
