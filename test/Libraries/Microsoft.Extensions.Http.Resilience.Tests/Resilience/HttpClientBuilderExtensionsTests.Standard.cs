// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Test.Helpers;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Polly.Testing;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test;

public sealed partial class HttpClientBuilderExtensionsTests : IDisposable
{
    private const string BuilderName = "Name";
    private readonly IHttpClientBuilder _builder;
    private ServiceProvider? _serviceProvider;

    public HttpClientBuilderExtensionsTests()
    {
        _builder = new ServiceCollection().AddHttpClient(BuilderName);
        _builder.Services.AddMetrics();
        _builder.Services.AddLogging();
    }

    public void Dispose()
        => _serviceProvider?.Dispose();

    private static Task<HttpResponseMessage> SendRequest(HttpClient client, string url, bool asynchronous)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

#if NET6_0_OR_GREATER
        if (asynchronous)
        {
            return client.SendAsync(request, default);
        }
        else
        {
            return Task.FromResult(client.Send(request, default));
        }
#else
        return client.SendAsync(request, default);
#endif
    }

    private HttpClient CreateClient(string name = BuilderName)
    {
        _serviceProvider ??= _builder.Services.BuildServiceProvider();
        return _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(name);
    }

    private static readonly IConfigurationSection _validConfigurationSection =
        ConfigurationStubFactory.Create(
            new Dictionary<string, string?>
            {
                { "StandardResilienceOptions:CircuitBreaker:FailureRatio", "0.1"},
                { "StandardResilienceOptions:AttemptTimeout:Timeout", "00:00:05"},
                { "StandardResilienceOptions:TotalRequestTimeout:Timeout", "00:00:20"},
            })
        .GetSection("StandardResilienceOptions");

    private static readonly IConfigurationSection _invalidConfigurationSection =
       ConfigurationStubFactory.Create(
            new Dictionary<string, string?>
            {
                { "StandardResilienceOptions:CircuitBreakerOptionsTypo:FailureRatio", "0.1"}
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

        Assert.Throws<ArgumentException>(() => AddStandardResilienceHandler(mode, builder, _emptyConfigurationSection, options => { }));
    }

    [InlineData(MethodArgs.Configuration)]
    [InlineData(MethodArgs.Configuration | MethodArgs.ConfigureMethod | MethodArgs.Builder)]
    [Theory]
    public void AddStandardResilienceHandler_ConfigurationPropertyWithTypo_Throws(MethodArgs mode)
    {
        var builder = new ServiceCollection().AddLogging().AddMetrics().AddHttpClient("test");

        AddStandardResilienceHandler(mode, builder, _invalidConfigurationSection, options => { });

        Assert.Throws<InvalidOperationException>(() => HttpClientBuilderExtensionsTests.GetPipeline(builder.Services, "test-standard"));
    }

    [Fact]
    public void AddStandardResilienceHandler_EnsureCorrectStrategies()
    {
        using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddMetrics()
            .AddHttpClient("test")
            .AddStandardResilienceHandler()
            .Services.BuildServiceProvider();

        var provider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<HttpKey>>();

        var descriptor = provider.GetPipeline<HttpResponseMessage>(new HttpKey("test-standard", string.Empty)).GetPipelineDescriptor();

        descriptor.Strategies.Should().HaveCount(5);
        descriptor.IsReloadable.Should().BeTrue();

        descriptor.Strategies[0].Options.Should().BeOfType<HttpRateLimiterStrategyOptions>();
        descriptor.Strategies[1].Options.Should().BeOfType<HttpTimeoutStrategyOptions>();
        descriptor.Strategies[2].Options.Should().BeOfType<HttpRetryStrategyOptions>();
        descriptor.Strategies[3].Options.Should().BeOfType<HttpCircuitBreakerStrategyOptions>();
        descriptor.Strategies[4].Options.Should().BeOfType<HttpTimeoutStrategyOptions>();
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void AddStandardResilienceHandler_EnsureValidated(bool wholePipeline)
    {
        var builder = new ServiceCollection().AddLogging().AddMetrics().AddHttpClient("test");

        AddStandardResilienceHandler(MethodArgs.ConfigureMethod, builder, null!, options =>
        {
            if (wholePipeline)
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(1);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
            }
            else
            {
                options.Retry.MaxRetryAttempts = -3;
            }
        });

        Assert.Throws<OptionsValidationException>(() => GetPipeline(builder.Services, "test-standard"));
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
        var builder = new ServiceCollection().AddLogging().AddMetrics().AddHttpClient("test");

        AddStandardResilienceHandler(mode, builder, _validConfigurationSection, options => { });

        var pipeline = GetPipeline(builder.Services, "test-standard");
        Assert.NotNull(pipeline);
    }

    [Theory]
#if NET6_0_OR_GREATER
    [CombinatorialData]
#else
    [InlineData(true)]
#endif
    public async Task DynamicReloads_Ok(bool asynchronous = true)
    {
        // arrange
        var requests = new List<HttpRequestMessage>();
        var config = ConfigurationStubFactory.Create(
            new()
            {
                { "standard:Retry:MaxRetryAttempts", "6" }
            },
            out var reloadAction).GetSection("standard");

        _builder.AddStandardResilienceHandler().Configure(config).Configure(options =>
        {
            options.Retry.Delay = TimeSpan.Zero;
            options.Retry.BackoffType = DelayBackoffType.Constant;
        });

        _builder.AddHttpMessageHandler(() => new TestHandlerStub((r, _) =>
        {
            requests.Add(r);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }));

        var client = CreateClient();

        // act && assert
        await SendRequest(client, "https://dummy", asynchronous);
        requests.Should().HaveCount(7);

        requests.Clear();
        reloadAction(new() { { "standard:Retry:MaxRetryAttempts", "10" } });

        await SendRequest(client, "https://dummy", asynchronous);
        requests.Should().HaveCount(11);
    }

    [Fact]
    public void AddStandardResilienceHandler_EnsureHttpClientTimeoutDisabled()
    {
        var builder = new ServiceCollection().AddLogging().AddMetrics().AddHttpClient("test").AddStandardResilienceHandler();

        using var client = builder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient("test");

        client.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
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

    private static ResiliencePipeline<HttpResponseMessage> GetPipeline(IServiceCollection services, string name)
    {
        var provider = services.BuildServiceProvider().GetRequiredService<ResiliencePipelineProvider<HttpKey>>();

        return provider.GetPipeline<HttpResponseMessage>(new HttpKey(name, string.Empty));
    }
}
