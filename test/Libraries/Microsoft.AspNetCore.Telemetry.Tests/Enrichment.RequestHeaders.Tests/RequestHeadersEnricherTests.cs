// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Telemetry.RequestHeaders.Test.Internals;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.RequestHeaders.Test;

public class RequestHeadersEnricherTests
{
    private const string HeaderKey1 = "X-RequestID";
    private const string HeaderKey2 = "Host";
    private const string HeaderKey3 = "NullHeader";
    private const string HeaderKey4 = "X-Platform";
    private const string RequestId = "RequestIdTestValue";
    private const string TestValue = "TestValue";

    private readonly Mock<IHttpContextAccessor> _accessorMock;
    private readonly Mock<IRedactorProvider> _redactorProviderMock;

    public RequestHeadersEnricherTests()
    {
        var headers = new HeaderDictionary
        {
            { HeaderKey1, RequestId },
            { HeaderKey2, string.Empty },
            { HeaderKey3, (string)null! },
            { HeaderKey4, TestValue },
        };

        var featureCollection = new FeatureCollection();

        var httpContextMock = new Mock<HttpContext>(MockBehavior.Strict);
        httpContextMock.SetupGet(c => c.Request.Headers).Returns(headers);
        httpContextMock.SetupGet(c => c.Request.HttpContext).Returns(httpContextMock.Object);
        httpContextMock.SetupGet(c => c.Features).Returns(featureCollection);

        _accessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        _accessorMock.SetupGet(r => r.HttpContext).Returns(httpContextMock.Object);

        var redactor = FakeRedactor.Create();
        _redactorProviderMock = new Mock<IRedactorProvider>(MockBehavior.Default);
        _redactorProviderMock.SetReturnsDefault<Redactor>(redactor);
    }

    [Fact]
    public void RequestHeadersEnricher_GivenDisabledEnricherOptions_HeaderKeysDataClasses_DoesNotEnrich()
    {
        // Arrange
        var options = new RequestHeadersLogEnricherOptions
        {
            HeadersDataClasses = new Dictionary<string, DataClassification>()
        };

        var enricher = new RequestHeadersLogEnricher(_accessorMock.Object, options.ToOptions(), _redactorProviderMock.Object);
        var enrichedProperties = new TestLogEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Properties;

        // Assert
        Assert.Empty(enrichedState);
    }

    [Fact]
    public void RequestHeadersEnricher_GivenEnricherOptions_HeaderKeysDataClasses_Enriches()
    {
        // Arrange
        var options = new RequestHeadersLogEnricherOptions
        {
            HeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { HeaderKey1, FakeClassifications.PrivateData },
                { HeaderKey4, FakeClassifications.PublicData }
            }
        };

        Mock<IRedactorProvider> redactorProviderMock = new Mock<IRedactorProvider>();
        redactorProviderMock.Setup(x => x.GetRedactor(FakeClassifications.PublicData))
            .Returns(new FakeRedactor());
        redactorProviderMock.Setup(x => x.GetRedactor(FakeClassifications.PrivateData))
            .Returns(FakeRedactor.Create(new FakeRedactorOptions { RedactionFormat = "redacted:{0}" }));

        var enricher = new RequestHeadersLogEnricher(_accessorMock.Object, options.ToOptions(), redactorProviderMock.Object);

        var enrichedProperties = new TestLogEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Properties;

        // Assert
        Assert.True(enrichedState.Count == 2);
        Assert.Equal($"redacted:{RequestId}", enrichedState[HeaderKey1].ToString());
        Assert.Equal(TestValue, enrichedState[HeaderKey4].ToString());
    }

    [Fact]
    public void RequestHeadersEnricher_GivenEnricherOptions_OneHeaderValueIsEmpty_HeaderKeysDataClasses_PartiallyEnriches()
    {
        // Arrange
        var options = new RequestHeadersLogEnricherOptions
        {
            HeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { HeaderKey1, FakeClassifications.PrivateData },
                { HeaderKey2, FakeClassifications.PublicData }
            }
        };

        Mock<IRedactorProvider> redactorProviderMock = new Mock<IRedactorProvider>();
        redactorProviderMock.Setup(x => x.GetRedactor(FakeClassifications.PrivateData))
            .Returns(FakeRedactor.Create(new FakeRedactorOptions { RedactionFormat = "REDACTED:{0}" }));
        var enricher = new RequestHeadersLogEnricher(_accessorMock.Object, options.ToOptions(), redactorProviderMock.Object);

        var enrichedProperties = new TestLogEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Properties;

        // Assert
        Assert.Single(enrichedState);
        Assert.Equal($"REDACTED:{RequestId}", enrichedState[HeaderKey1].ToString());
        Assert.False(enrichedState.ContainsKey(HeaderKey2));
    }

    [Fact]
    public void RequestHeadersEnricher_GivenEnricherOptions_OneHeaderValueIsNull_HeaderKeysDataClasses_PartiallyEnriches()
    {
        // Arrange
        var options = new RequestHeadersLogEnricherOptions
        {
            HeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { HeaderKey1, FakeClassifications.PublicData },
                { HeaderKey3, FakeClassifications.PublicData }
            }
        };
        var enricher = new RequestHeadersLogEnricher(_accessorMock.Object, options.ToOptions(), _redactorProviderMock.Object);

        var enrichedProperties = new TestLogEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Properties;

        // Assert
        Assert.Single(enrichedState);
        Assert.Equal(RequestId, enrichedState[HeaderKey1].ToString());
        Assert.False(enrichedState.ContainsKey(HeaderKey3));
    }

    [Fact]
    public void RequestHeadersEnricher_GivenEnricherOptions_OneHeaderValueIsMissing_HeaderKeysDataClasses_PartiallyEnriches()
    {
        // Arrange
        var headerKey2 = "header_does_not_exist";
        var options = new RequestHeadersLogEnricherOptions
        {
            HeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { HeaderKey1, FakeClassifications.PublicData },
                { headerKey2, FakeClassifications.PublicData }
            }
        };
        var enricher = new RequestHeadersLogEnricher(_accessorMock.Object, options.ToOptions(), _redactorProviderMock.Object);

        var enrichedProperties = new TestLogEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Properties;

        // Assert
        Assert.Equal(RequestId, enrichedState[HeaderKey1].ToString());
        Assert.False(enrichedState.ContainsKey(headerKey2));
    }

    [Fact]
    public void RequestHeadersEnricher_GivenNullHttpContext_HeaderKeysDataClasses_DoesNotEnrich()
    {
        // Arrange
        var options = new RequestHeadersLogEnricherOptions
        {
            HeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { HeaderKey1, FakeClassifications.PublicData }
            }
        };

        var accessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        accessorMock.SetupGet(r => r.HttpContext).Returns((HttpContext)null!);

        var enricher = new RequestHeadersLogEnricher(accessorMock.Object, options.ToOptions(), _redactorProviderMock.Object);

        var enrichedProperties = new TestLogEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Properties;

        // Assert
        Assert.Empty(enrichedState);
    }

    [Fact]
    public void RequestHeadersEnricher_GivenNullRequest_HeaderKeysDataClasses_DoesNotEnrich()
    {
        // Arrange
        var options = new RequestHeadersLogEnricherOptions
        {
            HeadersDataClasses = new Dictionary<string, DataClassification>
            {
                { HeaderKey1, FakeClassifications.PublicData }
            }
        };

        var featureCollection = new FeatureCollection();

        var httpContextMock = new Mock<HttpContext>(MockBehavior.Strict);
        httpContextMock.SetupGet(c => c.Request).Returns((HttpRequest)null!);
        httpContextMock.SetupGet(c => c.Features).Returns(featureCollection);

        var accessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        accessorMock.SetupGet(r => r.HttpContext).Returns(httpContextMock.Object);

        var enricher = new RequestHeadersLogEnricher(accessorMock.Object, options.ToOptions(), _redactorProviderMock.Object);

        var enrichedProperties = new TestLogEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Properties;

        // Assert
        Assert.Empty(enrichedState);
    }
}
