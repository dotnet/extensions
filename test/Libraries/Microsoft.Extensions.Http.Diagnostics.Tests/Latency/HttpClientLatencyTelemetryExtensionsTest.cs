// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Latency.Internal;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Latency.Test;

public class HttpClientLatencyTelemetryExtensionsTest
{
    [Fact]
    public void HttpClientLatencyTelemtry_NullParameter_ThrowsException()
    {
        var act = () => ((IServiceCollection)null!).AddHttpClientLatencyTelemetry();
        act.Should().Throw<ArgumentNullException>();

        act = () => Mock.Of<IServiceCollection>().AddHttpClientLatencyTelemetry((Action<HttpClientLatencyTelemetryOptions>)null!);
        act.Should().Throw<ArgumentNullException>();

        act = () => Mock.Of<IServiceCollection>().AddHttpClientLatencyTelemetry((IConfigurationSection)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HttpClientLatencyTelemtry_AddToServiceCollection()
    {
        using var sp = new ServiceCollection()
            .AddHttpClient()
            .AddNullLatencyContext()
            .AddHttpClientLatencyTelemetry()
            .BuildServiceProvider();

        var listener = sp.GetRequiredService<HttpRequestLatencyListener>();
        Assert.NotNull(listener);

        var latencyContext = sp.GetRequiredService<HttpClientLatencyContext>();
        Assert.NotNull(latencyContext);
        Assert.Null(latencyContext.Get());

        var options = sp.GetRequiredService<IOptions<HttpClientLatencyTelemetryOptions>>().Value;
        Assert.NotNull(options);
        Assert.True(options.EnableDetailedLatencyBreakdown);

        var handler = sp.GetRequiredService<HttpLatencyTelemetryHandler>();
        Assert.NotNull(handler);
    }

    [Fact]
    public void HttpClientLatencyTelemtry_AddToServiceCollection_CreatesClientSuccessfully()
    {
        using var sp = new ServiceCollection()
            .AddNullLatencyContext()
            .AddHttpClient()
            .AddHttpClientLatencyTelemetry()
            .BuildServiceProvider();

        using var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        Assert.NotNull(httpClient);
    }

    [Fact]
    public void HttpClientLatencyTelemtryExtensions_Add_InvokesConfig()
    {
        bool invoked = false;
        using var sp = new ServiceCollection()
            .AddNullLatencyContext()
            .AddHttpClientLatencyTelemetry(a =>
            {
                invoked = true;
                a.EnableDetailedLatencyBreakdown = false;
            })
            .BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<HttpClientLatencyTelemetryOptions>>().Value;
        Assert.NotNull(options);
        Assert.False(options.EnableDetailedLatencyBreakdown);
        Assert.True(invoked);
    }

    [Fact]
    public void RequestLatencyExtensions_Add_BindsToConfigSection()
    {
        HttpClientLatencyTelemetryOptions expectedOptions = new()
        {
            EnableDetailedLatencyBreakdown = false
        };

        var config = GetConfigSection(expectedOptions);
        using var sp = new ServiceCollection()
          .AddNullLatencyContext()
          .AddHttpClientLatencyTelemetry(config)
          .BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<HttpClientLatencyTelemetryOptions>>().Value;
        Assert.NotNull(options);
        Assert.Equal(expectedOptions.EnableDetailedLatencyBreakdown, options.EnableDetailedLatencyBreakdown);
    }

    [Fact]
    public async Task LatencyInfo_IsPopulated_WhenLoggerWrapsHandlersPipeline()
    {
        using var sp = new ServiceCollection()
            .AddLatencyContext()
            .AddRedaction()
            .AddHttpClientLatencyTelemetry()
            .AddHttpClient("test")
                .ConfigurePrimaryHttpMessageHandler(() => new ServerNameStubHandler("TestServer"))
                .AddExtendedHttpClientLogging(wrapHandlersPipeline: true)
            .Services
            .AddFakeLogging()
            .BuildServiceProvider();

        var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("test");

        using var response = await client.GetAsync("http://localhost/api");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var collector = sp.GetFakeLogCollector();
        var record = collector.LatestRecord;
        Assert.NotNull(record);

        var latencyInfo = record.GetStructuredStateValue("LatencyInfo");
        Assert.False(string.IsNullOrEmpty(latencyInfo));
        Assert.StartsWith("v1.0,", latencyInfo);
        Assert.Contains("TestServer", latencyInfo);
    }

    [Fact]
    public async Task LatencyInfo_IsPopulated_WithConfigurationSection_AndWrapHandlersPipeline()
    {
        var configSection = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Logging:LogBody", "true" }
            })
            .Build()
            .GetSection("Logging");

        using var sp = new ServiceCollection()
            .AddLatencyContext()
            .AddRedaction()
            .AddHttpClientLatencyTelemetry()
            .AddHttpClient("test")
                .ConfigurePrimaryHttpMessageHandler(() => new ServerNameStubHandler("TestServer"))
                .AddExtendedHttpClientLogging(configSection, wrapHandlersPipeline: true)
            .Services
            .AddFakeLogging()
            .BuildServiceProvider();

        var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("test");

        using var response = await client.GetAsync("http://localhost/api");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var collector = sp.GetFakeLogCollector();
        var record = collector.LatestRecord;
        Assert.NotNull(record);

        var latencyInfo = record.GetStructuredStateValue("LatencyInfo");
        Assert.False(string.IsNullOrEmpty(latencyInfo));
        Assert.StartsWith("v1.0,", latencyInfo);
    }

    [Fact]
    public async Task LatencyInfo_IsPopulated_WithActionConfiguration_AndWrapHandlersPipeline()
    {
        using var sp = new ServiceCollection()
            .AddLatencyContext()
            .AddRedaction()
            .AddHttpClientLatencyTelemetry()
            .AddHttpClient("test")
                .ConfigurePrimaryHttpMessageHandler(() => new ServerNameStubHandler("TestServer"))
                .AddExtendedHttpClientLogging(o => o.LogBody = true, wrapHandlersPipeline: false)
            .Services
            .AddFakeLogging()
            .BuildServiceProvider();

        var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("test");

        using var response = await client.GetAsync("http://localhost/api");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var collector = sp.GetFakeLogCollector();
        var record = collector.LatestRecord;
        Assert.NotNull(record);

        var latencyInfo = record.GetStructuredStateValue("LatencyInfo");
        Assert.False(string.IsNullOrEmpty(latencyInfo));
        Assert.StartsWith("v1.0,", latencyInfo);
    }

    [Fact]
    public async Task LatencyInfo_IsNotPresent_WhenLatencyTelemetryNotAdded()
    {
        using var sp = new ServiceCollection()
            .AddRedaction()
            .AddExtendedHttpClientLogging()
            .AddHttpClient("test")
                .ConfigurePrimaryHttpMessageHandler(() => new ServerNameStubHandler("TestServer"))
            .Services
            .AddFakeLogging()
            .BuildServiceProvider();

        var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("test");

        using var response = await client.GetAsync("http://localhost/api");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var collector = sp.GetFakeLogCollector();
        var record = collector.LatestRecord;
        Assert.NotNull(record);

        var latencyInfo = record.GetStructuredStateValue("LatencyInfo");
        Assert.True(string.IsNullOrEmpty(latencyInfo));
    }

    private static IConfigurationSection GetConfigSection(HttpClientLatencyTelemetryOptions options)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{nameof(HttpClientLatencyTelemetryOptions)}:{nameof(options.EnableDetailedLatencyBreakdown)}", options.EnableDetailedLatencyBreakdown.ToString(CultureInfo.InvariantCulture) },
            })
            .Build()
            .GetSection($"{nameof(HttpClientLatencyTelemetryOptions)}");
    }

    private sealed class ServerNameStubHandler(string serverName) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.TryAddWithoutValidation(TelemetryConstants.ServerApplicationNameHeader, serverName);
            return Task.FromResult(response);
        }
    }
}