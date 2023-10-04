// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Internal;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Telemetry;

public class DownstreamDependencyMetadataManagerTests : IDisposable
{
    private readonly IDownstreamDependencyMetadataManager _depMetadataManager;
    private readonly ServiceProvider _sp;

    public DownstreamDependencyMetadataManagerTests()
    {
        _sp = new ServiceCollection()
            .AddDownstreamDependencyMetadata(new BackslashDownstreamDependencyMetadata())
            .BuildServiceProvider();
        _depMetadataManager = _sp.GetRequiredService<IDownstreamDependencyMetadataManager>();
    }

    [Theory]
    [InlineData("DELETE", "https://anotherservice.net/singlebackslash", "StartingSingleBackslash", "/singlebackslash")]
    [InlineData("POST", "https://anotherservice.net/doublebackslash", "StartingDoublebackslash", "//doublebackslash")]
    [InlineData("PUT", "https://anotherservice.net/singlethensingle", "StartingSingleBackslashEndingSingleBackslash", "/singlethensingle/")]
    [InlineData("GET", "https://anotherservice.net/doublethensingle", "StartingDoublebackslashEndingSingleBackslash", "//doublethensingle/")]

    [InlineData("DELETE", "https://anotherservice.net//singlebackslash", "StartingSingleBackslash", "/singlebackslash")]
    [InlineData("POST", "https://anotherservice.net//doublebackslash", "StartingDoublebackslash", "//doublebackslash")]
    [InlineData("PUT", "https://anotherservice.net//singlethensingle", "StartingSingleBackslashEndingSingleBackslash", "/singlethensingle/")]
    [InlineData("GET", "https://anotherservice.net//doublethensingle", "StartingDoublebackslashEndingSingleBackslash", "//doublethensingle/")]

    [InlineData("DELETE", "https://anotherservice.net/singlebackslash/", "StartingSingleBackslash", "/singlebackslash")]
    [InlineData("POST", "https://anotherservice.net/doublebackslash/", "StartingDoublebackslash", "//doublebackslash")]
    [InlineData("PUT", "https://anotherservice.net/singlethensingle/", "StartingSingleBackslashEndingSingleBackslash", "/singlethensingle/")]
    [InlineData("GET", "https://anotherservice.net/doublethensingle/", "StartingDoublebackslashEndingSingleBackslash", "//doublethensingle/")]

    [InlineData("DELETE", "https://anotherservice.net//singlebackslash/", "StartingSingleBackslash", "/singlebackslash")]
    [InlineData("POST", "https://anotherservice.net//doublebackslash/", "StartingDoublebackslash", "//doublebackslash")]
    [InlineData("PUT", "https://anotherservice.net//singlethensingle/", "StartingSingleBackslashEndingSingleBackslash", "/singlethensingle/")]
    [InlineData("GET", "https://anotherservice.net//doublethensingle/", "StartingDoublebackslashEndingSingleBackslash", "//doublethensingle/")]
    public void GetRequestMetadata_RoutesRegisteredWithBackslashes_ShouldReturnHostMetadata(string httpMethod, string urlString, string expectedRequestName, string expectedRequestRoute)
    {
        using var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(method: httpMethod),
            RequestUri = new Uri(uriString: urlString)
        };

        var requestMetadata = _depMetadataManager.GetRequestMetadata(requestMessage);
        Assert.NotNull(requestMetadata);
        Assert.Equal(new BackslashDownstreamDependencyMetadata().DependencyName, requestMetadata.DependencyName);
        Assert.Equal(expectedRequestName, requestMetadata.RequestName);
        Assert.Equal(expectedRequestRoute, requestMetadata.RequestRoute);
        Assert.Equal(httpMethod, requestMetadata.MethodType);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sp.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
