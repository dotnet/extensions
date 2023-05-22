// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if FIXME
using System.Net.Mime;
using Microsoft.Extensions.Compliance.Classification;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if FIXME
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Telemetry;
#endif
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test;

public class HttpLoggingServiceExtensionsTest
{
    [Fact]
    public void ShouldThrow_WhenArgsNull()
    {
        var services = Mock.Of<IServiceCollection>();

        Assert.Throws<ArgumentNullException>(static () => HttpLoggingServiceExtensions.AddHttpLogging(null!));
        Assert.Throws<ArgumentNullException>(static () => HttpLoggingServiceExtensions.AddHttpLogEnricher<TestHttpLogEnricher>(null!));
        Assert.Throws<ArgumentNullException>(
            () => HttpLoggingServiceExtensions.AddHttpLogging(services, (Action<LoggingOptions>)null!));

        Assert.Throws<ArgumentNullException>(
            () => HttpLoggingServiceExtensions.AddHttpLogging(services, (IConfigurationSection)null!));
    }

#if FIXME
    [Fact]
    public void AddHttpLogging_WhenConfiguredUsingConfigurationSection_IsCorrect()
    {
        var services = new ServiceCollection();
        var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
        var configuration = builder.Build();

        services.AddHttpLogging(configuration.GetSection("HttpLogging"));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LoggingOptions>>().Value;

        Assert.True(options.LogRequestStart);
        Assert.True(options.RequestPathParameterRedactionMode == HttpRouteParameterRedactionMode.None);
        Assert.Equal(64 * 1024, options.BodySizeLimit);
        Assert.Equal(TimeSpan.FromSeconds(5), options.RequestBodyReadTimeout);
        Assert.Equal(IncomingPathLoggingMode.Structured, options.RequestPathLoggingMode);
        Assert.Equal(HttpRouteParameterRedactionMode.None, options.RequestPathParameterRedactionMode);
        Assert.Collection(options.RequestHeadersDataClasses, static x => Assert.Equal(HeaderNames.Accept, x.Key));
        Assert.Collection(options.RequestHeadersDataClasses, static x => Assert.Equal(DataClassification.None, x.Value));
        Assert.Collection(options.ResponseHeadersDataClasses, static x => Assert.Equal(HeaderNames.ContentType, x.Key));
        Assert.Collection(options.ResponseHeadersDataClasses, static x => Assert.Equal(SimpleClassifications.PrivateData, x.Value));
        Assert.Collection(options.RequestBodyContentTypes, static x => Assert.Equal(MediaTypeNames.Text.Plain, x));
        Assert.Collection(options.ResponseBodyContentTypes, static x => Assert.Equal(MediaTypeNames.Application.Json, x));

        Assert.Equal(2, options.RouteParameterDataClasses.Count);
        Assert.Contains(options.RouteParameterDataClasses, static x => x.Key == "userId" && x.Value == DataClass.EUII);
        Assert.Contains(options.RouteParameterDataClasses, static x => x.Key == "userContent" && x.Value == SimpleClassifications.PrivateData);
    }
#endif
}
