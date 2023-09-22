// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.FaultInjection;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Test;

public class HttpClientFaultInjectionExtensionsTest
{
    private readonly IConfiguration _configurationWithPolicyOptions;

    public HttpClientFaultInjectionExtensionsTest()
    {
        var builder = new ConfigurationBuilder().AddJsonFile("configs/appsettings.json");
        _configurationWithPolicyOptions = builder.Build();
    }

    [Fact]
    public void AddHttpClientFaultInjection_ShouldRegisterRequiredServices()
    {
        var services = new ServiceCollection();
        services
            .AddLogging()
            .AddHttpClientFaultInjection();

        using var serviceProvider = services.BuildServiceProvider();

        var chaosPolicyConfigProvider = serviceProvider.GetService<IFaultInjectionOptionsProvider>();
        Assert.IsAssignableFrom<IFaultInjectionOptionsProvider>(chaosPolicyConfigProvider);

        var policyFactory = serviceProvider.GetService<IChaosPolicyFactory>();
        Assert.IsAssignableFrom<IChaosPolicyFactory>(policyFactory);

        var httpPolicyFactory = serviceProvider.GetService<IHttpClientChaosPolicyFactory>();
        Assert.IsAssignableFrom<IHttpClientChaosPolicyFactory>(httpPolicyFactory);
    }

    [Fact]
    public void AddHttpClientFaultInjection_NullServices_ShouldThrow()
    {
        ServiceCollection? services = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddHttpClientFaultInjection());
    }

    [Fact]
    public void AddHttpClientFaultInjection_WithConfigurationSection_ShouldRegisterRequiredServices()
    {
        var services = new ServiceCollection();
        services
            .AddLogging()
            .AddHttpClientFaultInjection(_configurationWithPolicyOptions.GetSection("ChaosPolicyConfigurations"));

        using var serviceProvider = services.BuildServiceProvider();

        var chaosPolicyConfigProvider = serviceProvider.GetService<IFaultInjectionOptionsProvider>();
        Assert.IsAssignableFrom<IFaultInjectionOptionsProvider>(chaosPolicyConfigProvider);

        var policyFactory = serviceProvider.GetService<IChaosPolicyFactory>();
        Assert.IsAssignableFrom<IChaosPolicyFactory>(policyFactory);

        var httpPolicyFactory = serviceProvider.GetService<IHttpClientChaosPolicyFactory>();
        Assert.IsAssignableFrom<IHttpClientChaosPolicyFactory>(httpPolicyFactory);
    }

    [Fact]
    public void AddHttpClientFaultInjection_WithConfigurationSection_NullServices_ShouldThrow()
    {
        ServiceCollection? services = null!;

        Assert.Throws<ArgumentNullException>(
            () => services.AddHttpClientFaultInjection(_configurationWithPolicyOptions.GetSection("ChaosPolicyConfigurations")));
    }

    [Fact]
    public void AddHttpClientFaultInjection_NullConfigurationSection_ShouldThrow()
    {
        var services = new ServiceCollection();
        IConfigurationSection? configurationSection = null!;

        Assert.Throws<ArgumentNullException>(
            () => services.AddHttpClientFaultInjection(configurationSection));
    }

    [Fact]
    public void AddHttpClientFaultInjection_WithAction_ShouldRegisterRequiredServicesAndHttpMessageHandlers()
    {
        var services = new ServiceCollection();

        Action<HttpFaultInjectionOptionsBuilder> action =
            builder =>
                builder.Configure(_configurationWithPolicyOptions.GetSection("ChaosPolicyConfigurations"));
        services
            .AddLogging()
            .AddHttpClientFaultInjection(action);

        using var serviceProvider = services.BuildServiceProvider();

        var chaosPolicyConfigProvider = serviceProvider.GetService<IFaultInjectionOptionsProvider>();
        Assert.IsAssignableFrom<IFaultInjectionOptionsProvider>(chaosPolicyConfigProvider);

        var policyFactory = serviceProvider.GetService<IChaosPolicyFactory>();
        Assert.IsAssignableFrom<IChaosPolicyFactory>(policyFactory);

        var httpPolicyFactory = serviceProvider.GetService<IHttpClientChaosPolicyFactory>();
        Assert.IsAssignableFrom<IHttpClientChaosPolicyFactory>(httpPolicyFactory);

        var httpClientFactoryOptions = serviceProvider.GetRequiredService<IOptions<HttpClientFactoryOptions>>().Value;
        Assert.True(httpClientFactoryOptions.HttpMessageHandlerBuilderActions.Count > 0);
    }

    [Fact]
    public void AddHttpClientFaultInjection_NullAction_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action<HttpFaultInjectionOptionsBuilder>? action = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddHttpClientFaultInjection(action));
    }

    [Fact]
    public void AddHttpClientFaultInjection_WithAction_NullServices_ShouldThrow()
    {
        ServiceCollection? services = null!;

        Action<HttpFaultInjectionOptionsBuilder> action = builder => { };
        Assert.Throws<ArgumentNullException>(
            () => services.AddHttpClientFaultInjection(action));
    }

    [Fact]
    public async Task AddHttpClientFaultInjection_FaultInjectionShouldWork()
    {
        var chaosPolicyOptionsGroup = new ChaosPolicyOptionsGroup
        {
            HttpResponseInjectionPolicyOptions = new HttpResponseInjectionPolicyOptions
            {
                Enabled = true,
                FaultInjectionRate = 1.0
            }
        };

        Action<HttpFaultInjectionOptionsBuilder> action =
            builder =>
                builder.Configure(option =>
                {
                    option.ChaosPolicyOptionsGroups = new Dictionary<string, ChaosPolicyOptionsGroup>
                    {
                            { "HttpClientClass", chaosPolicyOptionsGroup }
                    };
                });
        var httpClient = SetupHttpClientWithFaultInjection(action);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:12345");
        var response = await httpClient.SendAsync(request);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(response.RequestMessage, request);
    }

    [Fact]
    public async Task AddHttpClientFaultInjection_FaultInjectionWithHttpContent()
    {
        var chaosPolicyOptionsGroup = new ChaosPolicyOptionsGroup
        {
            HttpResponseInjectionPolicyOptions = new HttpResponseInjectionPolicyOptions
            {
                Enabled = true,
                FaultInjectionRate = 1.0,
                StatusCode = HttpStatusCode.OK,
                HttpContentKey = "TestPayload"
            }
        };
        using var testContent = new StringContent("Test Content");
        Action<HttpFaultInjectionOptionsBuilder> action =
            builder =>
                builder.Configure(option =>
                {
                    option.ChaosPolicyOptionsGroups = new Dictionary<string, ChaosPolicyOptionsGroup>
                    {
                            { "HttpClientClass", chaosPolicyOptionsGroup }
                    };
                })
                .AddHttpContent("TestPayload", testContent);
        var httpClient = SetupHttpClientWithFaultInjection(action);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:12345");
        var response = await httpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(testContent, response.Content);
        Assert.Equal(response.RequestMessage, request);
    }

    private class HttpClientClass
    {
    }

    private class TestMessageHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("OK") });
        }
    }

    private static HttpClient SetupHttpClientWithFaultInjection(Action<HttpFaultInjectionOptionsBuilder> configure)
    {
        var services = new ServiceCollection();
        var httpClientIdentifier = "HttpClientClass";

        services
            .AddLogging()
            .AddHttpClientFaultInjection(configure);
        services
            .AddHttpClient<HttpClientClass>()
            .AddHttpMessageHandler(() => new TestMessageHandler());

        using var serviceProvider = services.BuildServiceProvider();
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        return clientFactory.CreateClient(httpClientIdentifier);
    }
}
