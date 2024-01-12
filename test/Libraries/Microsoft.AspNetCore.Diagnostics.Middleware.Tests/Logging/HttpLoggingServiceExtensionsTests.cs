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
            new KeyValuePair<string, string?>("HttpLogging:ExcludePathStartsWith:[0]","/path0toexclude"),
            new KeyValuePair<string, string?>("HttpLogging:ExcludePathStartsWith:[1]","/path1toexclude"),
        });
        var configuration = builder.Build();
        services.AddHttpLoggingRedaction(configuration.GetSection("HttpLogging"));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LoggingRedactionOptions>>().Value;

        Assert.Equal(IncomingPathLoggingMode.Structured, options.RequestPathLoggingMode);
        Assert.Equal(HttpRouteParameterRedactionMode.None, options.RequestPathParameterRedactionMode);

        Assert.Contains("/path0toexclude", options.ExcludePathStartsWith);
        Assert.Contains("/path1toexclude", options.ExcludePathStartsWith);
    }

    [Fact]
    public void AddHttpLogging_CanConfigureDataClasses()
    {
        var services = new ServiceCollection();
        services.AddHttpLoggingRedaction(o =>
        {
            o.RouteParameterDataClasses = new Dictionary<string, DataClassification>
            {
                { "one", new DataClassification("Taxonomy1", "Value1") },
            };

            o.RequestHeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { "two", new DataClassification("Taxonomy2", "Value2") },
            };

            o.ResponseHeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { "three", new DataClassification("Taxonomy3", "Value3") },
            };
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LoggingRedactionOptions>>().Value;

        Assert.Single(options.RouteParameterDataClasses);
        Assert.Equal("Taxonomy1", options.RouteParameterDataClasses["one"].TaxonomyName);
        Assert.Equal("Value1", options.RouteParameterDataClasses["one"].Value);

        Assert.Single(options.RequestHeadersDataClasses);
        Assert.Equal("Taxonomy2", options.RequestHeadersDataClasses["two"].TaxonomyName);
        Assert.Equal("Value2", options.RequestHeadersDataClasses["two"].Value);

        Assert.Single(options.ResponseHeadersDataClasses);
        Assert.Equal("Taxonomy3", options.ResponseHeadersDataClasses["three"].TaxonomyName);
        Assert.Equal("Value3", options.ResponseHeadersDataClasses["three"].Value);
    }
}
#endif
