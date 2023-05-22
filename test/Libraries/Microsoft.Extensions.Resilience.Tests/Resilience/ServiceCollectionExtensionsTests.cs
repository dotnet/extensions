// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Test.Helpers;
using Microsoft.Extensions.Telemetry.Metering;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test;

public class ServiceCollectionExtensionsTests : ResilienceTestHelper
{
    private readonly Mock<Resilience.Internal.IPolicyPipelineBuilder<string>> _pipelineBuilder = new(MockBehavior.Strict);

    public ServiceCollectionExtensionsTests()
    {
        Services.TryAddSingleton(_pipelineBuilder.Object);
    }

    [Fact]
    public void AddResiliencePipeline_EnsureValidationAndCorrectResult()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddResiliencePipeline<string>(null!));
        Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddResiliencePipeline<string>(null!, "pipelineName"));

        var result = services.AddResiliencePipeline<string>("test");

        Assert.NotNull(result.Services.FirstOrDefault(s => s.ServiceType == typeof(IResiliencePipelineFactory)));
        Assert.Equal("test", result.PipelineName);
        Assert.Equal(services, result.Services);
    }

    [Fact]
    public void AddResiliencePipeline_EnsureNecessaryServicesAdded()
    {
        var services = new ServiceCollection();
        services.AddLogging().RegisterMetering().AddSingleton(System.TimeProvider.System);
        services.AddResiliencePipeline<string>("test");
        services.AddResiliencePipeline<bool>("test");

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IResiliencePipelineFactory>();

        // string based
        provider.GetRequiredService<IPolicyFactory>();
        provider.GetRequiredService<Resilience.Internal.IPolicyPipelineBuilder<string>>();
        Assert.NotEqual(provider.GetRequiredService<Resilience.Internal.IPolicyPipelineBuilder<string>>(), provider.GetRequiredService<Resilience.Internal.IPolicyPipelineBuilder<string>>());

        // bool based
        provider.GetRequiredService<IPolicyFactory>();
        provider.GetRequiredService<Resilience.Internal.IPolicyPipelineBuilder<bool>>();
        Assert.NotEqual(provider.GetRequiredService<Resilience.Internal.IPolicyPipelineBuilder<bool>>(), provider.GetRequiredService<Resilience.Internal.IPolicyPipelineBuilder<bool>>());

        Assert.NotEqual(provider.GetRequiredService<IPipelineMetering>(), provider.GetRequiredService<IPipelineMetering>());
    }

    [Fact]
    public void AddResiliencePipeline_EnsureAllOptionsAutomaticallyValidated()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterMetering();

        Assert.Throws<ArgumentNullException>(() => services.AddResiliencePipeline<string>(null!));
        Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddResiliencePipeline<string>(null!, "pipelineName"));

        var result = services.AddResiliencePipeline<string>("test");
        var factory = services.BuildServiceProvider().GetRequiredService<IResiliencePipelineFactory>();

        Assert.Throws<OptionsValidationException>(() => factory.CreatePipeline<string>("test", string.Empty));
        var error = Assert.Throws<OptionsValidationException>(() => factory.CreatePipeline<string>("not-configured", string.Empty));
#if NETCOREAPP
        Assert.Equal("BuilderActions: This resilience pipeline is not configured. Each resilience pipeline must include at least one policy. Field path: not-configured.BuilderActions", error.Message);
#endif
    }
}
