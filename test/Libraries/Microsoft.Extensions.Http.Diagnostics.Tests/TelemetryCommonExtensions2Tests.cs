// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Diagnostics;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Internal.Test;

public class TelemetryCommonExtensions2Tests
{
    [Fact]
    public void GetDependencyName_DependencyNameMissing_ReturnsUnknown()
    {
        var requestMetadata = new RequestMetadata
        {
            RequestName = "sampleRequest",
            RequestRoute = "/v1/users/{userId}/chats"
        };

        Assert.Equal(TelemetryConstants.Unknown, requestMetadata.DependencyName);
    }

    [Fact]
    public void GetRequestRoute_RequestRouteMissing_ReturnsUnknown()
    {
        var requestMetadata = new RequestMetadata
        {
            DependencyName = "testDependency",
            RequestName = "sampleRequest"
        };

        Assert.Equal(TelemetryConstants.Unknown, requestMetadata.RequestRoute);
    }

    [Fact]
    public void AddHttpRouteProcessor_Registers_RouterParserAndFormatter()
    {
        var sp = new ServiceCollection().AddFakeRedaction().AddHttpRouteProcessor().BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<IHttpRouteParser>());
        Assert.NotNull(sp.GetRequiredService<IHttpRouteFormatter>());
    }

    [Fact]
    public void AddHttpHeadersRedactor_NullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddHttpHeadersRedactor());
    }

    [Fact]
    public void AddHttpHeadersRedactor_Registers_HttpHeadersRedactor()
    {
        var sp = new ServiceCollection().AddFakeRedaction().AddHttpHeadersRedactor().BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<IHttpHeadersRedactor>());
    }

    [Fact]
    public void AsynContext_SetRequestMetadata_ValidRequestMetadata_CorrectlySet()
    {
        var serviceCollection = new ServiceCollection();
        var sp = serviceCollection.AddOutgoingRequestContext().BuildServiceProvider();

        var requestMetadataContext = sp.GetService<IOutgoingRequestContext>();

        var metadata = new RequestMetadata
        {
            DependencyName = "testDependency",
            RequestName = "sampleRequest",
            RequestRoute = "/v1/users/{userId}/chats"
        };

        requestMetadataContext?.SetRequestMetadata(metadata);

        var extractedMetadata = requestMetadataContext?.RequestMetadata;
        Assert.NotNull(extractedMetadata);
        Assert.Equal(metadata.DependencyName, extractedMetadata!.DependencyName);
        Assert.Equal(metadata.RequestName, extractedMetadata!.RequestName);
        Assert.Equal(metadata.RequestRoute, extractedMetadata!.RequestRoute);
    }

    [Fact]
    public void AsynContext_SetRequestMetadata_EmptyRequestMetadata_CorrectlySets()
    {
        var serviceCollection = new ServiceCollection();
        var sp = serviceCollection.AddOutgoingRequestContext().BuildServiceProvider();

        var requestMetadataContext = sp.GetService<IOutgoingRequestContext>()!;
        var metadata = new RequestMetadata();

        requestMetadataContext.SetRequestMetadata(metadata);

        var extractedMetadata = requestMetadataContext.RequestMetadata;
        Assert.NotNull(extractedMetadata);
        Assert.Equal(TelemetryConstants.Unknown, extractedMetadata!.DependencyName);
        Assert.Equal(TelemetryConstants.Unknown, extractedMetadata!.RequestName);
        Assert.Equal(TelemetryConstants.Unknown, extractedMetadata!.RequestRoute);
    }
}
