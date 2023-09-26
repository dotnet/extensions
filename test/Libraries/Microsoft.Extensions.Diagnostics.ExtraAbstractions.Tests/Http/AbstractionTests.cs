// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Http.Diagnostics.Test;

public class AbstractionTests
{
    [Fact]
    public void RequestMetadata_DefaultConsrtuctor_HasDefaultValues()
    {
        var requestMetadata = new RequestMetadata();

        Assert.Equal("GET", requestMetadata.MethodType);
        Assert.Equal(TelemetryConstants.Unknown, requestMetadata.DependencyName);
        Assert.Equal(TelemetryConstants.Unknown, requestMetadata.RequestName);
        Assert.Equal(TelemetryConstants.Unknown, requestMetadata.RequestRoute);
    }

    [Fact]
    public void RequestMetadata_ParameterizedConsrtuctor_HasProvidedValues()
    {
        var requestMetadata = new RequestMetadata("POST", "/v1/temp/route/{routeId}", "TestRequest")
        {
            DependencyName = "MyDependency"
        };

        Assert.Equal("POST", requestMetadata.MethodType);
        Assert.Equal("MyDependency", requestMetadata.DependencyName);
        Assert.Equal("TestRequest", requestMetadata.RequestName);
        Assert.Equal("/v1/temp/route/{routeId}", requestMetadata.RequestRoute);
    }

    [Fact]
    public void Ensure_TelemetryConstantValuesAreNotChanged()
    {
        Assert.Equal("R9-RequestMetadata", TelemetryConstants.RequestMetadataKey);
        Assert.Equal("unknown", TelemetryConstants.Unknown);
        Assert.Equal("REDACTED", TelemetryConstants.Redacted);
    }
}
