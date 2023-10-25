// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Http.Diagnostics.Test;

public class TelemetryCommonExtensionsTests
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
}
