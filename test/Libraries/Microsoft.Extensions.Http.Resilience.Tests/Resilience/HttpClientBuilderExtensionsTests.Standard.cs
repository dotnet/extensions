// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Test.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Metering;
using Polly;
using Polly.Registry;
using Polly.Retry;
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

    private HttpClient CreateClient(string name = BuilderName) => _builder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient(name);

    private static readonly IConfigurationSection _validConfigurationSection =
        ConfigurationStubFactory.Create(
            new Dictionary<string, string?>
            {
                { "StandardResilienceOptions:CircuitBreakerOptions:FailureThreshold", "0.1"},
                { "StandardResilienceOptions:AttemptTimeoutOptions:Timeout", "00:00:05"},
                { "StandardResilienceOptions:TotalRequestTimeoutOptions:Timeout", "00:00:20"},
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

#if NET8_0_OR_GREATER
        // Whilst these API are marked as NET6_0_OR_GREATER we don't build .NET 6.0,
        // and as such the API is available in .NET 8 onwards.
        Assert.Throws<InvalidOperationException>(() => HttpClientBuilderExtensionsTests.GetStrategy(builder.Services, $"test-standard"));
#else
        GetStrategy(builder.Services, $"test-standard").Should().NotBeNull();
#endif
    }

    [Fact]
    public void AddStandardResilienceHandler_EnsureCorrectStrategies()
    {
        var called = false;
        var builder = new ServiceCollection().AddLogging().RegisterMetering().AddHttpClient("test");
        builder.Services.TryAddTransient(_ =>
        {
            return new ResilienceStrategyBuilder
            {
                OnCreatingStrategy = strategies =>
                {
                    strategies.Should().HaveCount(5);
                    strategies[0].GetType().Name.Should().Contain("RateLimiter");
                    strategies[1].GetType().Name.Should().Contain("Timeout");
                    strategies[2].GetType().Name.Should().Contain("Retry");
                    strategies[3].GetType().Name.Should().Contain("CircuitBreaker");
                    strategies[4].GetType().Name.Should().Contain("Timeout");

                    called = true;
                }
            };
        });

        builder.AddStandardResilienceHandler();

        _ = builder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient("test");
        called.Should().BeTrue();
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
                options.TotalRequestTimeoutOptions.Timeout = TimeSpan.FromSeconds(2);
                options.AttemptTimeoutOptions.Timeout = TimeSpan.FromSeconds(1);
            }
            else
            {
                options.RetryOptions.RetryCount = -3;
            }
        });

        Assert.Throws<OptionsValidationException>(() => GetStrategy(builder.Services, $"test-standard"));
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

        var pipeline = GetStrategy(builder.Services, $"test-standard");
        Assert.NotNull(pipeline);
    }

    [Fact]
    public async Task DynamicReloads_Ok()
    {
        // arrange
        var requests = new List<HttpRequestMessage>();
        var config = ConfigurationStubFactory.Create(
            new()
            {
                { "standard:RetryOptions:RetryCount", "6" }
            },
            out var reloadAction).GetSection("standard");

        _builder.AddStandardResilienceHandler().Configure(config).Configure(options =>
        {
            options.RetryOptions.BaseDelay = TimeSpan.Zero;
            options.RetryOptions.BackoffType = RetryBackoffType.Constant;
        });
        _builder.AddHttpMessageHandler(() => new TestHandlerStub((r, _) =>
        {
            requests.Add(r);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }));

        var client = CreateClient();

        // act && assert
        await client.GetAsync("https://dummy");
        requests.Should().HaveCount(7);

        requests.Clear();
        reloadAction(new() { { "standard:RetryOptions:RetryCount", "10" } });

        await client.GetAsync("https://dummy");
        requests.Should().HaveCount(11);
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

    private static ResilienceStrategy<HttpResponseMessage> GetStrategy(IServiceCollection services, string name)
    {
        var provider = services.BuildServiceProvider().GetRequiredService<ResilienceStrategyProvider<HttpKey>>();

        return provider.GetStrategy<HttpResponseMessage>(new HttpKey(name, string.Empty));
    }
}
