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
using Microsoft.Extensions.Resilience.FaultInjection;
using Microsoft.Extensions.Telemetry.Metrics;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Test;

public class HttpClientBuilderExtensionsTest
{
    private readonly IConfiguration _configurationWithPolicyOptions;

    public HttpClientBuilderExtensionsTest()
    {
        var builder = new ConfigurationBuilder().AddJsonFile("configs/appsettings.json");
        _configurationWithPolicyOptions = builder.Build();
    }

    [Fact]
    public async Task HttpClientBuilder_AddFaultInjectionPolicyHandler_WithOptionsName_NoPreviousContext_ShouldWork()
    {
        var httpClient = SetupHttpClientWithFaultInjection("TestGroup1");

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:12345");
        var response = await httpClient.SendAsync(request);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task HttpClientBuilder_AddFaultInjectionPolicyHandler_WithOptionsName_WithPreviousContext_ShouldSucceed()
    {
        var httpClient = SetupHttpClientWithFaultInjection("TestGroup1");

        var context = new Context();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:12345");
        request.SetPolicyExecutionContext(context);
        var response = await httpClient.SendAsync(request);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public void AddFaultInjectionPolicyHandler_OptionsGroupNameNull_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action<HttpFaultInjectionOptionsBuilder> action = builder => { };
        services
            .AddLogging()
            .RegisterMetrics()
            .AddHttpClientFaultInjection(action);

        var builder = services.AddHttpClient<HttpClientClass>();
        Assert.Throws<ArgumentNullException>(() => builder.AddFaultInjectionPolicyHandler(null!));
    }

    [Fact]
    public void AddFaultInjectionPolicyHandler_HttpClientBuilderNull_ShouldThrow()
    {
        IHttpClientBuilder? builder = null!;
        Assert.Throws<ArgumentNullException>(() => builder.AddFaultInjectionPolicyHandler("TestGroup1"));
    }

    [Fact]
    public async Task AddWeightedFaultInjectionPolicyHandlers_WithWeightAssignmentsConfigAction()
    {
        var services = new ServiceCollection();
        var httpClientIdentifier = "HttpClientClass";
        var chaosPolicyOptionsGroup1 = new ChaosPolicyOptionsGroup
        {
            HttpResponseInjectionPolicyOptions = new HttpResponseInjectionPolicyOptions
            {
                Enabled = true,
                FaultInjectionRate = 1.0,
                StatusCode = HttpStatusCode.GatewayTimeout
            }
        };
        var chaosPolicyOptionsGroup2 = new ChaosPolicyOptionsGroup
        {
            HttpResponseInjectionPolicyOptions = new HttpResponseInjectionPolicyOptions
            {
                Enabled = true,
                FaultInjectionRate = 1.0,
                StatusCode = HttpStatusCode.ServiceUnavailable
            }
        };

        Action<HttpFaultInjectionOptionsBuilder> action = builder =>
            builder
                .Configure(option =>
                {
                    option.ChaosPolicyOptionsGroups = new Dictionary<string, ChaosPolicyOptionsGroup>
                    {
                            { "TestA", chaosPolicyOptionsGroup1 },
                            { "TestB", chaosPolicyOptionsGroup2 },
                    };
                });

        services
            .AddLogging()
            .RegisterMetrics()
            .AddHttpClientFaultInjection(action);
        services
            .AddHttpClient<HttpClientClass>()
            .AddWeightedFaultInjectionPolicyHandlers(weightAssignmentsOptions =>
            {
                weightAssignmentsOptions.WeightAssignments.Add("TestA", 50);
                weightAssignmentsOptions.WeightAssignments.Add("TestB", 50);
            })
            .AddHttpMessageHandler(() => new TestMessageHandler());

        var clientFactory = services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();
        var httpClient = clientFactory.CreateClient(httpClientIdentifier);

        for (int i = 0; i < 100; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:12345");
            var response = await httpClient.SendAsync(request);

            Assert.False(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode == HttpStatusCode.GatewayTimeout || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        }
    }

    [Fact]
    public async Task AddWeightedFaultInjectionPolicyHandlers_WithWeightAssignmentsConfigSection()
    {
        var services = new ServiceCollection();
        var httpClientIdentifier = "HttpClientClass";
        var chaosPolicyOptionsGroup1 = new ChaosPolicyOptionsGroup
        {
            HttpResponseInjectionPolicyOptions = new HttpResponseInjectionPolicyOptions
            {
                Enabled = true,
                FaultInjectionRate = 1.0,
                StatusCode = HttpStatusCode.GatewayTimeout
            }
        };
        var chaosPolicyOptionsGroup2 = new ChaosPolicyOptionsGroup
        {
            HttpResponseInjectionPolicyOptions = new HttpResponseInjectionPolicyOptions
            {
                Enabled = true,
                FaultInjectionRate = 1.0,
                StatusCode = HttpStatusCode.ServiceUnavailable
            }
        };

        Action<HttpFaultInjectionOptionsBuilder> action = builder =>
            builder
                .Configure(option =>
                {
                    option.ChaosPolicyOptionsGroups = new Dictionary<string, ChaosPolicyOptionsGroup>
                    {
                            { "TestA", chaosPolicyOptionsGroup1 },
                            { "TestB", chaosPolicyOptionsGroup2 },
                    };
                });

        services
            .AddLogging()
            .RegisterMetrics()
            .AddHttpClientFaultInjection(action);
        services
            .AddHttpClient<HttpClientClass>()
            .AddWeightedFaultInjectionPolicyHandlers(_configurationWithPolicyOptions.GetSection("FaultPolicyWeightAssignments"))
            .AddHttpMessageHandler(() => new TestMessageHandler());

        var clientFactory = services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();
        var httpClient = clientFactory.CreateClient(httpClientIdentifier);

        for (int i = 0; i < 100; i++)
        {
            var context = new Context
            {
                ["TestField"] = "Test123"
            };

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:12345");
            request.SetPolicyExecutionContext(context);
            var response = await httpClient.SendAsync(request);

            Assert.Equal("Test123", request.GetPolicyExecutionContext()!["TestField"]);
            Assert.False(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode == HttpStatusCode.GatewayTimeout || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        }
    }

    [Fact]
    public void AddWeightedFaultInjectionPolicyHandlers_WeightAssignmentOptionsNull_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action<HttpFaultInjectionOptionsBuilder> action = builder => { };
        services
            .AddLogging()
            .RegisterMetrics()
            .AddHttpClientFaultInjection(action);

        var builder = services.AddHttpClient<HttpClientClass>();
        Assert.Throws<ArgumentNullException>(() => builder.AddWeightedFaultInjectionPolicyHandlers((Action<FaultPolicyWeightAssignmentsOptions>)null!));
        Assert.Throws<ArgumentNullException>(() => builder.AddWeightedFaultInjectionPolicyHandlers((IConfigurationSection)null!));
    }

    [Fact]
    public void AddWeightedFaultInjectionPolicyHandlers_HttpClientBuilderNull_ShouldThrow()
    {
        IHttpClientBuilder? builder = null!;
        Assert.Throws<ArgumentNullException>(() => builder.AddWeightedFaultInjectionPolicyHandlers(options => { }));
        Assert.Throws<ArgumentNullException>(() => builder.AddWeightedFaultInjectionPolicyHandlers(_configurationWithPolicyOptions.GetSection("FaultPolicyWeightAssignments")));
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

    private static System.Net.Http.HttpClient SetupHttpClientWithFaultInjection(string chaosPolicyOptionsGroupName)
    {
        var services = new ServiceCollection();
        var httpClientIdentifier = "HttpClientClass";
        var chaosPolicyOptionsGroup1 = new ChaosPolicyOptionsGroup
        {
            HttpResponseInjectionPolicyOptions = new HttpResponseInjectionPolicyOptions
            {
                Enabled = true,
                FaultInjectionRate = 1.0
            }
        };

        Action<HttpFaultInjectionOptionsBuilder> action = builder =>
            builder
                .Configure(option =>
                {
                    option.ChaosPolicyOptionsGroups = new Dictionary<string, ChaosPolicyOptionsGroup>
                    {
                            { chaosPolicyOptionsGroupName, chaosPolicyOptionsGroup1 }
                    };
                });
        services
            .AddLogging()
            .RegisterMetrics()
            .AddHttpClientFaultInjection(action);
        services
            .AddHttpClient<HttpClientClass>()
            .AddFaultInjectionPolicyHandler(chaosPolicyOptionsGroupName)
            .AddHttpMessageHandler(() => new TestMessageHandler());

        var clientFactory = services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();
        return clientFactory.CreateClient(httpClientIdentifier);
    }
}
