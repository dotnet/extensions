// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Logging.Test.Internal;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Logging.Test;

public class HttpClientLoggingExtensionsTest
{
    private readonly Fixture _fixture;

    public HttpClientLoggingExtensionsTest()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void AddHttpClientLogging_AnyArgumentIsNull_Throws()
    {
        var act = () => ((IHttpClientBuilder)null!).AddExtendedHttpClientLogging();
        act.Should().Throw<ArgumentNullException>();

        act = () => ((IHttpClientBuilder)null!).AddExtendedHttpClientLogging(_ => { });
        act.Should().Throw<ArgumentNullException>();

        act = () => ((IHttpClientBuilder)null!).AddExtendedHttpClientLogging(Mock.Of<IConfigurationSection>());
        act.Should().Throw<ArgumentNullException>();

        act = () => Mock.Of<IHttpClientBuilder>().AddExtendedHttpClientLogging((Action<LoggingOptions>)null!);
        act.Should().Throw<ArgumentNullException>();

        act = () => Mock.Of<IHttpClientBuilder>().AddExtendedHttpClientLogging((IConfigurationSection)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddHttpClientLogging_ServiceCollection_AnyArgumentIsNull_Throws()
    {
        var act = () => ((IServiceCollection)null!).AddExtendedHttpClientLogging();
        act.Should().Throw<ArgumentNullException>();

        act = () => ((IServiceCollection)null!).AddExtendedHttpClientLogging(_ => { });
        act.Should().Throw<ArgumentNullException>();

        act = () => ((IServiceCollection)null!).AddExtendedHttpClientLogging(Mock.Of<IConfigurationSection>());
        act.Should().Throw<ArgumentNullException>();

        act = () => Mock.Of<IServiceCollection>().AddExtendedHttpClientLogging((Action<LoggingOptions>)null!);
        act.Should().Throw<ArgumentNullException>();

        act = () => Mock.Of<IServiceCollection>().AddExtendedHttpClientLogging((IConfigurationSection)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddHttpClientLogEnricher_AnyArgumentIsNull_Throws()
    {
        var act = () => ((IServiceCollection)null!).AddHttpClientLogEnricher<EmptyEnricher>();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddHttpClientLogging_ConfiguredOptionsWithNamedClient_ShouldNotBeSame()
    {
        var services = new ServiceCollection();

        using var provider = services
            .AddHttpClient("test1")
            .AddExtendedHttpClientLogging(options => options.BodyReadTimeout = TimeSpan.FromSeconds(1))
            .Services
            .AddHttpClient("test2")
            .AddExtendedHttpClientLogging(options => options.BodyReadTimeout = TimeSpan.FromSeconds(2))
            .Services
            .BuildServiceProvider();

        var optionsFirst = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get("test1");
        var optionsSecond = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get("test2");
        optionsFirst.Should().NotBeNull();
        optionsSecond.Should().NotBeNull();
        optionsFirst.Should().NotBeEquivalentTo(optionsSecond);
        optionsFirst.BodyReadTimeout.Should().Be(TimeSpan.FromSeconds(1));
        optionsSecond.BodyReadTimeout.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AddHttpClientLogging_ConfiguredOptionsWithTypedClient_ShouldNotBeSame()
    {
        var services = new ServiceCollection();

        using var provider = services
            .AddHttpClient<ITestHttpClient1, TestHttpClient1>()
            .AddExtendedHttpClientLogging(options => options.BodyReadTimeout = TimeSpan.FromSeconds(1))
            .Services
            .AddHttpClient<ITestHttpClient2, TestHttpClient2>()
            .AddExtendedHttpClientLogging(options => options.BodyReadTimeout = TimeSpan.FromSeconds(2))
            .Services
            .BuildServiceProvider();

        var optionsFirst = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get(nameof(ITestHttpClient1));
        var optionsSecond = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get(nameof(ITestHttpClient2));
        optionsFirst.Should().NotBeNull();
        optionsSecond.Should().NotBeNull();
        optionsFirst.Should().NotBeEquivalentTo(optionsSecond);
        optionsFirst.BodyReadTimeout.Should().Be(TimeSpan.FromSeconds(1));
        optionsSecond.BodyReadTimeout.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AddHttpClientLogging_DefaultOptions_CreatesOptionsCorrectly()
    {
        var services = new ServiceCollection();

        using var provider = services
            .AddHttpClient("")
            .AddExtendedHttpClientLogging(o => o.RequestHeadersDataClasses.Add("test1", FakeClassifications.PrivateData))
            .Services
            .AddHttpClient("")
            .AddExtendedHttpClientLogging(o => o.RequestHeadersDataClasses.Add("test2", FakeClassifications.PrivateData))
            .Services
            .BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<LoggingOptions>>().Value;
        options.RequestHeadersDataClasses.Should().HaveCount(2);
        options.RequestHeadersDataClasses.Should().ContainKeys(new List<string> { "test1", "test2" });
        options.RequestHeadersDataClasses.Should().ContainValues(new List<DataClassification> { FakeClassifications.PrivateData });
    }

    [Fact]
    public void AddHttpClientLogging_GivenActionDelegate_RegistersInDi()
    {
        var requestBodyContentType = "application/json";
        var responseBodyContentType = "application/json";
        var requestHeader = _fixture.Create<string>();
        var responseHeader = _fixture.Create<string>();
        var bodyReadTimeout = TimeSpan.FromSeconds(1);
        var bodySizeLimit = 100;
        var formatRequestPath = _fixture.Create<OutgoingPathLoggingMode>();
        var formatRequestPathParameters = _fixture.Create<HttpRouteParameterRedactionMode>();
        var logStart = _fixture.Create<bool>();
        var paramToRedact = new KeyValuePair<string, DataClassification>("userId", FakeClassifications.PrivateData);

        var services = new ServiceCollection();

        services
            .AddHttpClient("test")
            .AddExtendedHttpClientLogging(options =>
            {
                options.RequestBodyContentTypes.Add(requestBodyContentType);
                options.ResponseBodyContentTypes.Add(responseBodyContentType);
                options.BodyReadTimeout = bodyReadTimeout;
                options.BodySizeLimit = bodySizeLimit;
                options.RequestPathLoggingMode = formatRequestPath;
                options.RequestPathParameterRedactionMode = formatRequestPathParameters;
                options.RequestHeadersDataClasses.Add(requestHeader, FakeClassifications.PrivateData);
                options.ResponseHeadersDataClasses.Add(responseHeader, FakeClassifications.PrivateData);
                options.RouteParameterDataClasses.Add(paramToRedact);
                options.LogRequestStart = logStart;
            });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get("test");

        options.Should().NotBeNull();
        options.RequestBodyContentTypes.Should().ContainSingle();
        options.RequestBodyContentTypes.Should().Contain(requestBodyContentType);
        options.ResponseBodyContentTypes.Should().ContainSingle();
        options.ResponseBodyContentTypes.Should().Contain(responseBodyContentType);
        options.BodyReadTimeout.Should().Be(bodyReadTimeout);
        options.BodySizeLimit.Should().Be(bodySizeLimit);
        options.RequestPathLoggingMode.Should().Be(formatRequestPath);
        options.RequestPathParameterRedactionMode.Should().Be(formatRequestPathParameters);
        options.RequestHeadersDataClasses.Should().ContainSingle();
        options.RequestHeadersDataClasses.Should().Contain(requestHeader, FakeClassifications.PrivateData);
        options.ResponseHeadersDataClasses.Should().ContainSingle();
        options.ResponseHeadersDataClasses.Should().Contain(responseHeader, FakeClassifications.PrivateData);
        options.RouteParameterDataClasses.Should().ContainSingle();
        options.RouteParameterDataClasses.Should().Contain(paramToRedact);
        options.LogRequestStart.Should().Be(logStart);
    }

    [Fact]
    public async Task AddHttpClientLogging_GivenInvalidOptions_Throws()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services =>
            {
                services
                    .AddFakeRedaction()
                    .AddHttpClient("test")
                    .AddExtendedHttpClientLogging(options =>
                    {
                        options.BodyReadTimeout = TimeSpan.Zero;
                        options.BodySizeLimit = -1;
                    });
            })
            .Build();

        var act = async () => await host.StartAsync();
        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(59)]
    [InlineData(17)]
    public void AddHttpClientLogging_GivenConfigurationSection_SetsTimeoutCorrectly(int seconds)
    {
        var timeoutValue = TimeSpan.FromSeconds(seconds);

        using var provider = new ServiceCollection()
            .AddHttpClient("test")
            .AddExtendedHttpClientLogging(TestConfiguration.GetHttpClientLoggingConfigurationSection(timeoutValue))
            .Services
            .BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get("test");

        options.Should().NotBeNull();
        options.BodyReadTimeout.Should().Be(timeoutValue);
    }

    [Fact]
    public void AddHttpClientLogEnricher_RegistersEnricherInDI()
    {
        using var provider = new ServiceCollection()
            .AddHttpClientLogEnricher<EmptyEnricher>()
            .BuildServiceProvider();

        var enricherRegistered = provider.GetService<IHttpClientLogEnricher>();

        enricherRegistered.Should().NotBeNull();
        enricherRegistered.Should().BeOfType<EmptyEnricher>();
    }

    [Fact]
    public void AddHttpClientLogging_ServiceCollection_GivenActionDelegate_RegistersInDi()
    {
        var requestBodyContentType = "application/json";
        var responseBodyContentType = "application/json";
        var requestHeader = _fixture.Create<string>();
        var responseHeader = _fixture.Create<string>();
        var bodyReadTimeout = TimeSpan.FromSeconds(1);
        var bodySizeLimit = 100;
        var formatRequestPath = _fixture.Create<OutgoingPathLoggingMode>();
        var formatRequestPathParameters = _fixture.Create<HttpRouteParameterRedactionMode>();
        var logStart = _fixture.Create<bool>();
        var paramToRedact = new KeyValuePair<string, DataClassification>("userId", FakeClassifications.PrivateData);

        var services = new ServiceCollection();

        services
            .AddFakeRedaction()
            .AddHttpClient()
            .AddExtendedHttpClientLogging(options =>
            {
                options.RequestBodyContentTypes.Add(requestBodyContentType);
                options.ResponseBodyContentTypes.Add(responseBodyContentType);
                options.BodyReadTimeout = bodyReadTimeout;
                options.BodySizeLimit = bodySizeLimit;
                options.RequestPathLoggingMode = formatRequestPath;
                options.RequestPathParameterRedactionMode = formatRequestPathParameters;
                options.RequestHeadersDataClasses.Add(requestHeader, FakeClassifications.PrivateData);
                options.ResponseHeadersDataClasses.Add(responseHeader, FakeClassifications.PrivateData);
                options.RouteParameterDataClasses.Add(paramToRedact);
                options.LogRequestStart = logStart;
            });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LoggingOptions>>().Value;

        options.Should().NotBeNull();
        options.RequestBodyContentTypes.Should().ContainSingle();
        options.RequestBodyContentTypes.Should().Contain(requestBodyContentType);
        options.ResponseBodyContentTypes.Should().ContainSingle();
        options.ResponseBodyContentTypes.Should().Contain(responseBodyContentType);
        options.BodyReadTimeout.Should().Be(bodyReadTimeout);
        options.BodySizeLimit.Should().Be(bodySizeLimit);
        options.RequestPathLoggingMode.Should().Be(formatRequestPath);
        options.RequestPathParameterRedactionMode.Should().Be(formatRequestPathParameters);
        options.RequestHeadersDataClasses.Should().ContainSingle();
        options.RequestHeadersDataClasses.Should().Contain(requestHeader, FakeClassifications.PrivateData);
        options.ResponseHeadersDataClasses.Should().ContainSingle();
        options.ResponseHeadersDataClasses.Should().Contain(responseHeader, FakeClassifications.PrivateData);
        options.RouteParameterDataClasses.Should().ContainSingle();
        options.RouteParameterDataClasses.Should().Contain(paramToRedact);
        options.LogRequestStart.Should().Be(logStart);

        using var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
        Assert.NotNull(httpClient);
    }

    [Fact]
    public async Task AddHttpClientLogging_ServiceCollection_GivenInvalidOptions_Throws()
    {
        using var provider = new ServiceCollection()
            .AddFakeRedaction()
            .AddHttpClient()
            .AddExtendedHttpClientLogging(options =>
            {
                options.BodyReadTimeout = TimeSpan.Zero;
                options.BodySizeLimit = -1;
            })
            .BuildServiceProvider();

        var act = () =>
            provider
                .GetRequiredService<IHostedService>()
                .StartAsync(CancellationToken.None);
        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Fact]
    public void AddHttpClientLogging_ServiceCollectionAndHttpClientBuilder_DoesNotDuplicate()
    {
        const string ClientName = "test";

        using var provider = new ServiceCollection()
            .AddFakeRedaction()
            .AddHttpClient(ClientName)
            .AddExtendedHttpClientLogging(x =>
            {
                x.BodySizeLimit = 100500;
                x.RequestHeadersDataClasses.Add(ClientName, FakeClassifications.PublicData);
            }).Services
            .AddExtendedHttpClientLogging(x =>
            {
                x.BodySizeLimit = 347;
                x.RequestHeadersDataClasses.Add("default", FakeClassifications.PrivateData);
            })
            .BuildServiceProvider();

        EnsureSingleLogger<HttpClientLogger>(provider, ClientName);
    }

    [Fact]
    public void AddHttpClientLogging_HttpClientBuilderAndServiceCollection_DoesNotDuplicate()
    {
        const string ClientName = "test";

        using var provider = new ServiceCollection()
            .AddFakeRedaction()
            .AddExtendedHttpClientLogging()
            .AddHttpClient(ClientName)
            .AddExtendedHttpClientLogging()
            .Services.BuildServiceProvider();

        EnsureSingleLogger<HttpClientLogger>(provider, ClientName);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(59)]
    [InlineData(17)]
    public void AddHttpClientLogging_ServiceCollection_GivenConfigurationSection_SetsTimeoutCorrectly(int seconds)
    {
        var timeoutValue = TimeSpan.FromSeconds(seconds);

        using var provider = new ServiceCollection()
            .AddFakeRedaction()
            .AddHttpClient()
            .AddExtendedHttpClientLogging(TestConfiguration.GetHttpClientLoggingConfigurationSection(timeoutValue))
            .BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptions<LoggingOptions>>().Value;

        options.Should().NotBeNull();
        options.BodyReadTimeout.Should().Be(timeoutValue);

        using var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
        Assert.NotNull(httpClient);
    }

    [Fact]
    public void AddHttpClientLogging_ServiceCollection_CreatesClientSuccessfully()
    {
        using var sp = new ServiceCollection()
            .AddFakeRedaction()
            .AddHttpClient()
            .AddExtendedHttpClientLogging()
            .BuildServiceProvider();

        using var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        Assert.NotNull(httpClient);
    }

    private static void EnsureSingleLogger<T>(IServiceProvider serviceProvider, string serviceKey)
        where T : IHttpClientLogger
    {
        var loggers = serviceProvider.GetServices<T>();
        loggers.Should().ContainSingle();

        var keyedLoggers = serviceProvider.GetKeyedServices<T>(serviceKey);
        keyedLoggers.Should().ContainSingle();
    }
}
