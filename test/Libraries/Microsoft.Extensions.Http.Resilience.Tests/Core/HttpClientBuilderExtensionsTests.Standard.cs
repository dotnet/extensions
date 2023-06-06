// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Tests.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Telemetry.Metering;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test;

public sealed partial class HttpClientBuilderExtensionsTests
{
    private const string BuilderName = "Name";
    private readonly IHttpClientBuilder _builder;

    public HttpClientBuilderExtensionsTests()
    {
        _builder = new ServiceCollection().AddHttpClient(BuilderName);
        _builder.Services.RegisterMetering();
        _builder.Services.AddLogging();
    }

    private static readonly IConfigurationSection _validConfigurationSection =
        ConfigurationStubFactory.Create(
            new Dictionary<string, string?>
            {
                { "StandardResilienceOptions:CircuitBreakerOptions:FailureThreshold", "0.1"},
                { "StandardResilienceOptions:AttemptTimeoutOptions:TimeoutInterval", "00:00:05"},
                { "StandardResilienceOptions:TotalRequestTimeoutOptions:TimeoutInterval", "00:00:20"},
            })
        .GetSection("StandardResilienceOptions");

    private static readonly IConfigurationSection _invalidConfigurationSection =
       ConfigurationStubFactory.Create(
            new Dictionary<string, string?>
            {
                { "StandardResilienceOptions:CircuitBreakerOptionsTypo:FailureThreshold", "0.1"}
            })
        .GetSection("StandardResilienceOptions");

    private static readonly IConfigurationSection _emptyConfigurationSection =
        ConfigurationStubFactory.CreateEmpty().GetSection(string.Empty);

    [Flags]
    public enum MethodArgs
    {
        None = 0,

        ConfigureMethod = 1 << 0,

        ConfigureMethodWithServiceProvider = 1 << 1,

        Configuration = 1 << 2,

        Builder = 1 << 3,
    }

    [InlineData(MethodArgs.None)]
    [InlineData(MethodArgs.ConfigureMethod)]
    [InlineData(MethodArgs.Configuration)]
    [InlineData(MethodArgs.Configuration | MethodArgs.ConfigureMethod)]
    [Theory]
    public void AddStandardResilienceHandler_NullBuilder_Throws(MethodArgs mode)
    {
        IHttpClientBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(() => AddStandardResilienceHandler(mode, builder, _validConfigurationSection, options => { }));
    }

    [InlineData(MethodArgs.ConfigureMethod)]
    [InlineData(MethodArgs.Configuration | MethodArgs.ConfigureMethod)]
    [Theory]
    public void AddStandardResilienceHandler_NullConfigureMethod_Throws(MethodArgs mode)
    {
        var builder = new ServiceCollection().AddHttpClient("test");

        Assert.Throws<ArgumentNullException>(() => AddStandardResilienceHandler(mode, builder, _validConfigurationSection, null!));

    }

    [InlineData(MethodArgs.Configuration)]
    [InlineData(MethodArgs.Configuration | MethodArgs.ConfigureMethod)]
    [Theory]
    public void AddStandardResilienceHandler_NullConfiguration_Throws(MethodArgs mode)
    {
        var builder = new ServiceCollection().AddHttpClient("test");

        Assert.Throws<ArgumentNullException>(() => AddStandardResilienceHandler(mode, builder, null!, options => { }));
    }

    [InlineData(MethodArgs.Configuration)]
    [InlineData(MethodArgs.Configuration | MethodArgs.ConfigureMethod)]
    [Theory]
    public void AddStandardResilienceHandler_NullConfigurationSectionContent_Throws(MethodArgs mode)
    {
        var builder = new ServiceCollection().AddHttpClient("test");

        Assert.Throws<ArgumentNullException>(() => AddStandardResilienceHandler(mode, builder, _emptyConfigurationSection, options => { }));
    }

    [InlineData(MethodArgs.Configuration)]
    [InlineData(MethodArgs.Configuration | MethodArgs.ConfigureMethod | MethodArgs.Builder)]
    [Theory]
    public void AddStandardResilienceHandler_ConfigurationPropertyWithTypo_Throws(MethodArgs mode)
    {
        var builder = new ServiceCollection().AddLogging().RegisterMetering().AddHttpClient("test");

        AddStandardResilienceHandler(mode, builder, _invalidConfigurationSection, options => { });

        var provider = builder.Services.BuildServiceProvider().GetRequiredService<IResiliencePipelineProvider>();
#if NET8_0_OR_GREATER
        // Whilst these API are marked as NET6_0_OR_GREATER we don't build .NET 6.0,
        // and as such the API is available in .NET 8 onwards.
        Assert.Throws<InvalidOperationException>(() => provider.GetPipeline<HttpResponseMessage>($"test-standard"));
#else
        var pipeline = provider.GetPipeline<HttpResponseMessage>($"test-standard");
        Assert.NotNull(pipeline);
#endif
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void AddStandardResilienceHandler_EnsureValidated(bool wholePipeline)
    {
        var builder = new ServiceCollection().AddLogging().RegisterMetering().AddHttpClient("test");

        AddStandardResilienceHandler(MethodArgs.ConfigureMethod, builder, null!, options =>
        {
            if (wholePipeline)
            {
                options.TotalRequestTimeoutOptions.TimeoutInterval = TimeSpan.FromSeconds(2);
                options.AttemptTimeoutOptions.TimeoutInterval = TimeSpan.FromSeconds(1);
            }
            else
            {
                options.BulkheadOptions.MaxQueuedActions = -1;
            }
        });

        var provider = builder.Services.BuildServiceProvider().GetRequiredService<IResiliencePipelineProvider>();

        Assert.Throws<OptionsValidationException>(() => provider.GetPipeline<HttpResponseMessage>($"test-standard"));
    }

    [InlineData(MethodArgs.None)]
    [InlineData(MethodArgs.ConfigureMethod)]
    [InlineData(MethodArgs.Configuration)]
    [InlineData(MethodArgs.Configuration | MethodArgs.ConfigureMethod)]
    [InlineData(MethodArgs.Builder | MethodArgs.ConfigureMethodWithServiceProvider)]
    [InlineData(MethodArgs.ConfigureMethod | MethodArgs.Builder)]
    [InlineData(MethodArgs.Configuration | MethodArgs.Builder)]
    [InlineData(MethodArgs.Configuration | MethodArgs.ConfigureMethod | MethodArgs.Builder)]
    [Theory]
    public void AddStandardResilienceHandler_EnsureConfigured(MethodArgs mode)
    {
        var builder = new ServiceCollection().AddLogging().RegisterMetering().AddHttpClient("test");

        AddStandardResilienceHandler(mode, builder, _validConfigurationSection, options => { });

        var provider = builder.Services.BuildServiceProvider().GetRequiredService<IResiliencePipelineProvider>();

        var pipeline = provider.GetPipeline<HttpResponseMessage>($"test-standard");
        Assert.NotNull(pipeline);
    }

    private static void AddStandardResilienceHandler(
        MethodArgs mode,
        IHttpClientBuilder builder,
        IConfigurationSection configuration,
        Action<HttpStandardResilienceOptions> configureMethod)
    {
        _ = mode switch
        {
            MethodArgs.None => builder.AddStandardResilienceHandler(),
            MethodArgs.Configuration | MethodArgs.Builder => builder.AddStandardResilienceHandler().Configure(configuration),
            MethodArgs.ConfigureMethod | MethodArgs.Builder => builder.AddStandardResilienceHandler().Configure(configureMethod),
            MethodArgs.ConfigureMethodWithServiceProvider | MethodArgs.Builder => builder.AddStandardResilienceHandler().Configure((options, serviceProvider) =>
            {
                serviceProvider.Should().NotBeNull();
                configureMethod(options);
            }),
            MethodArgs.Configuration | MethodArgs.ConfigureMethod | MethodArgs.Builder => builder.AddStandardResilienceHandler().Configure(configuration).Configure(configureMethod),
            MethodArgs.Configuration | MethodArgs.ConfigureMethod => builder.AddStandardResilienceHandler().Configure(configuration).Configure(configureMethod),
            MethodArgs.Configuration => builder.AddStandardResilienceHandler(configuration),
            MethodArgs.ConfigureMethod => builder.AddStandardResilienceHandler(configureMethod),
            _ => throw new NotSupportedException()
        };
    }
}
