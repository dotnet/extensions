// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
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
}
#endif
