// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public class HttpLoggingServiceExtensionsTests
{
    [Fact]
    public void ShouldThrow_WhenArgsNull()
    {
        var services = Mock.Of<IServiceCollection>();

        Assert.Throws<ArgumentNullException>(static () => HttpLoggingServiceCollectionExtensions.AddHttpLogEnricher<TestHttpLogEnricher>(null!));

        Assert.Throws<ArgumentNullException>(
            () => HttpLoggingServiceCollectionExtensions.AddHttpLoggingRedaction(services, (IConfigurationSection)null!));
    }

    [Fact]
    public void AddHttpLogging_WhenConfiguredUsingConfigurationSection_IsCorrect()
    {
        var services = new ServiceCollection();
        var builder = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>("HttpLogging:RequestPathLoggingMode", "Structured"),
            new KeyValuePair<string, string?>("HttpLogging:RequestPathParameterRedactionMode","None"),
            new KeyValuePair<string, string?>("HttpLogging:RouteParameterDataClasses:userId:taxonomyName","user1"),
            new KeyValuePair<string, string?>("HttpLogging:RouteParameterDataClasses:userId:value","1"),
            new KeyValuePair<string, string?>("HttpLogging:RouteParameterDataClasses:userContent:taxonomyName","context2"),
            new KeyValuePair<string, string?>("HttpLogging:RouteParameterDataClasses:userContent:value","2"),
            new KeyValuePair<string, string?>("HttpLogging:RequestHeadersDataClasses:Accept:taxonomyName","accept3"),
            new KeyValuePair<string, string?>("HttpLogging:RequestHeadersDataClasses:Accept:value","3"),
            new KeyValuePair<string, string?>("HttpLogging:ResponseHeadersDataClasses:Content-Type:taxonomyName","content4"),
            new KeyValuePair<string, string?>("HttpLogging:ResponseHeadersDataClasses:Content-Type:value","4"),
            new KeyValuePair<string, string?>("HttpLogging:ExcludePathStartsWith:[0]","/path0toexclude"),
            new KeyValuePair<string, string?>("HttpLogging:ExcludePathStartsWith:[1]","/path1toexclude"),
        });
        var configuration = builder.Build();
        services.AddHttpLoggingRedaction(configuration.GetSection("HttpLogging"));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LoggingRedactionOptions>>().Value;

        Assert.Equal(IncomingPathLoggingMode.Structured, options.RequestPathLoggingMode);
        Assert.Equal(HttpRouteParameterRedactionMode.None, options.RequestPathParameterRedactionMode);

        Assert.Equal(2, options.RouteParameterDataClasses.Count);
        Assert.Contains(options.RouteParameterDataClasses, static x => x.Key == "userId" && x.Value == new DataClassification("user1", 1));
        Assert.Contains(options.RouteParameterDataClasses, static x => x.Key == "userContent" && x.Value == new DataClassification("context2", 2));

        Assert.Contains(options.RequestHeadersDataClasses, static x => x.Key == HeaderNames.Accept && x.Value == new DataClassification("accept3", 3));
        Assert.Contains(options.ResponseHeadersDataClasses, static x => x.Key == HeaderNames.ContentType && x.Value == new DataClassification("content4", 4));

        Assert.Contains("/path0toexclude", options.ExcludePathStartsWith);
        Assert.Contains("/path1toexclude", options.ExcludePathStartsWith);
    }
}
#endif
