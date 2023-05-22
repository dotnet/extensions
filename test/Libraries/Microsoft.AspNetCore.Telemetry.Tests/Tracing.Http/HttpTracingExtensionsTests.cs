// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
#if !NETCOREAPP3_1_OR_GREATER
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Internal;
using Moq;
#endif
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using OpenTelemetry.Trace;
using Xunit;

using MSOptions = Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Telemetry.Test;

public class HttpTracingExtensionsTests
{
    [Fact]
    public void AddHttpTracing_GivenNullArgument_Throws()
    {
        var configRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configSection = configRoot.GetSection("HttpTracingOptions");

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpTracing());

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpTracing(options => { }));

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpTracing(configSection));

        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddOpenTelemetry().WithTracing(builder =>
                builder.AddHttpTracing((Action<HttpTracingOptions>)null!)));

        Assert.Throws<ArgumentNullException>(() =>
            services.AddOpenTelemetry().WithTracing(builder =>
                builder.AddHttpTracing((IConfigurationSection)null!)));
    }

    [Fact]
    public void AddHttpTraceEnricher_GivenNullArgument_Throws()
    {
        var testEnricher = new TestHttpTraceEnricher(MSOptions.Options.Create(new HttpTracingOptions()));

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpTraceEnricher<TestHttpTraceEnricher>());

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddHttpTraceEnricher(testEnricher));

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddHttpTraceEnricher<TestHttpTraceEnricher>());

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddHttpTraceEnricher(testEnricher));
    }

    [Theory]
    [CombinatorialData]
    public void AddHttpTracing_RegistersHttpUrlProcessor(bool isLoggerPresent)
    {
        using var host = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .Configure(hostBuilder =>
            {
                if (isLoggerPresent)
                {
                    hostBuilder.ConfigureLogging(builder => builder.AddFakeLogging());
                }
            })
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder.AddHttpTracing()))
            .Build();

        Assert.NotNull(host.Services.GetService<ILogger<HttpUrlRedactionProcessor>>());
    }

    [Fact]
    public void AddHttpTracing_GivenRedactorProviderAndTagsToRedact_RegistersHttpUrlProcessorWithRedactorProvider()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpTracing(options => options.RouteParameterDataClasses.Add("TestTag", SimpleClassifications.PrivateData))))
            .Build();

        Assert.NotNull(host.Services.GetRequiredService<HttpUrlRedactionProcessor>());
    }

    [Fact]
    public void AddHttpTracing_GivenNoRedactorProviderAndHasTagsToRedact_Throws()
    {
        using var host = FakeHost.CreateBuilder(new FakeHostOptions { FakeRedaction = false, ValidateOnBuild = false })
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddHttpTracing(options => options.RouteParameterDataClasses.Add("TestTag", SimpleClassifications.PrivateData))))
            .Build();

        Assert.Throws<InvalidOperationException>(
            () => host.Services.GetService<HttpUrlRedactionProcessor>());
    }

#if !NETCOREAPP3_1_OR_GREATER
    [Fact]
    public void HttpTraceEnrichmentProcessor_OnEndWithNullRequest_ShouldNotCallEnrich()
    {
        var httpEnricher = new TestHttpTraceEnricher(MSOptions.Options.Create(new HttpTracingOptions()));
        var enrichers = new List<IHttpTraceEnricher>
        {
            httpEnricher
        };

        using var httpEnrichmentProcessor = new HttpTraceEnrichmentProcessor(GetRedactionProcessor(), enrichers);
        using Activity activity = new Activity("Test");
        httpEnrichmentProcessor.OnEnd(activity);
        Assert.False(httpEnricher.IsEnrichCalled);
    }

    [Fact]
    public void HttpTraceEnrichmentProcessor_OnEndWithRequest_ShouldCallEnrich()
    {
        var httpContextMock = new Mock<HttpContext>(MockBehavior.Default);
        httpContextMock.Setup(h => h.Features.Get<IEndpointFeature>()).Returns((IEndpointFeature)null!);
        var requestMock = new Mock<HttpRequest>();
        requestMock.SetupGet(r => r.HttpContext).Returns(httpContextMock.Object);
        var httpEnricher = new TestHttpTraceEnricher(MSOptions.Options.Create(new HttpTracingOptions()));
        var enrichers = new List<IHttpTraceEnricher>
        {
            httpEnricher
        };

        using var httpEnrichmentProcessor = new HttpTraceEnrichmentProcessor(GetRedactionProcessor(), enrichers);
        using Activity activity = new Activity("Test");

        activity.SetCustomProperty(Constants.CustomPropertyHttpRequest, requestMock.Object);
        httpEnrichmentProcessor.OnEnd(activity);
        Assert.True(httpEnricher.IsEnrichCalled);
    }

    private static HttpUrlRedactionProcessor GetRedactionProcessor()
    {
        var options = MSOptions.Options.Create(new HttpTracingOptions());
        var builder = new ServiceCollection()
            .AddFakeRedaction(options => options.RedactionFormat = "Redacted:{0}")
            .AddHttpRouteProcessor()
            .AddHttpRouteUtilities()
            .BuildServiceProvider();

        var formatter = builder.GetService<IHttpRouteFormatter>()!;
        var parser = builder.GetService<IHttpRouteParser>()!;
        var utility = builder.GetService<IIncomingHttpRouteUtility>()!;
        var logger = new Mock<ILogger<HttpUrlRedactionProcessor>>().Object;

        return new HttpUrlRedactionProcessor(options, formatter, parser, utility, logger);
    }
#endif
}
