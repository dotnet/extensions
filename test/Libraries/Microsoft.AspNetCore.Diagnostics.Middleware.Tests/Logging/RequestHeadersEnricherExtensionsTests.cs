// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public class RequestHeadersEnricherExtensionsTests
{
    [Fact]
    public void RequestHeadersLogEnricher_GivenAnyNullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddRequestHeadersLogEnricher());

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddRequestHeadersLogEnricher(_ => { }));

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddRequestHeadersLogEnricher(Mock.Of<IConfigurationSection>()));

        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddRequestHeadersLogEnricher((IConfigurationSection)null!));

        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddRequestHeadersLogEnricher((Action<RequestHeadersLogEnricherOptions>)null!));
    }

    [Fact]
    public void RequestHeadersLogEnricher_GivenOptions_HeaderKeysWithDataClass_NoRedaction_Throws()
    {
        using var sp = new ServiceCollection()
            .AddRequestHeadersLogEnricher(e => e.HeadersDataClasses.Add("TestKey", DataClassification.None))
            .BuildServiceProvider();

        Assert.Throws<ArgumentNullException>(() => sp.GetRequiredService<ILogEnricher>());
    }

    [Fact]
    public void RequestHeadersLogEnricher_GivenNoArguments_WithRedaction_RegistersInDI()
    {
        // Act
        using var serviceProvider = new ServiceCollection()
            .AddRequestHeadersLogEnricher()
            .AddFakeRedaction()
            .BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetRequiredService<ILogEnricher>());
    }

    [Fact]
    public void RequestHeadersLogEnricher_GivenHeaderKeysWithDataClassAndRedaction_RegistersInDI()
    {
        // Act
        using var serviceProvider = new ServiceCollection()
            .AddRequestHeadersLogEnricher(e =>
            {
                e.HeadersDataClasses.Add("TestKey", DataClassification.None);
            })
            .AddFakeRedaction()
            .BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetRequiredService<ILogEnricher>());
        var options = serviceProvider.GetRequiredService<IOptions<RequestHeadersLogEnricherOptions>>().Value;
        Assert.NotNull(options);
        Assert.NotNull(options.HeadersDataClasses);
        Assert.Single(options.HeadersDataClasses);
    }

    [Fact]
    public void RequestHeadersLogEnricher_GivenConfigurationSectionAndRedaction_RegistersInDI()
    {
        // Arrange
        var configRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Section:HeadersDataClasses:TestKey:taxonomyName", "simple"),
                new KeyValuePair<string, string?>("Section:HeadersDataClasses:TestKey:value", "1")
            })
            .Build();

        // Act
        using var serviceProvider = new ServiceCollection()
            .AddRequestHeadersLogEnricher(configRoot.GetSection("Section"))
            .AddFakeRedaction()
            .BuildServiceProvider();

        // Assert
        var enricher = serviceProvider.GetRequiredService<ILogEnricher>();
        Assert.NotNull(enricher);
        Assert.IsType<RequestHeadersLogEnricher>(enricher);

        var options = serviceProvider.GetRequiredService<IOptions<RequestHeadersLogEnricherOptions>>().Value;
        Assert.NotNull(options);
        Assert.NotNull(options.HeadersDataClasses);
        Assert.Single(options.HeadersDataClasses);
        Assert.Equal(new DataClassification("simple", 1), options.HeadersDataClasses["TestKey"]);
    }
}
