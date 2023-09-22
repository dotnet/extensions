// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Diagnostics;
using Moq;
using Moq.Protected;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.AutoClient.Test;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "IDisposable inside mock setups")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S1067:Expressions should not be too complex", Justification = "Mock conditions")]
public class TelemetryTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handlerMock = new(MockBehavior.Strict);
    private readonly Mock<IHttpClientFactory> _factoryMock = new(MockBehavior.Strict);

    private readonly IRequestMetadataTestClient _sutClient;
    private readonly IRequestMetadataTestApi _sutApi;
    private readonly ICustomRequestMetadataTestClient _sutCustom;
    private readonly ServiceProvider _provider;

    public TelemetryTests()
    {
        _factoryMock.Setup(m => m.CreateClient("MyClient")).Returns(new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://example.com/")
        });

        var services = new ServiceCollection();
        services.AddSingleton(_ => _factoryMock.Object);
        services.AddRequestMetadataTestClient();
        services.AddRequestMetadataTestApi();
        services.AddCustomRequestMetadataTestClient();
        _provider = services.BuildServiceProvider();

        _sutClient = _provider.GetRequiredService<IRequestMetadataTestClient>();
        _sutApi = _provider.GetRequiredService<IRequestMetadataTestApi>();
        _sutCustom = _provider.GetRequiredService<ICustomRequestMetadataTestClient>();
    }

    [Fact]
    public async Task ClientDependencyComplexPath()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users/myUser?search=searchParam" &&
                message.GetRequestMetadata()!.RequestRoute == "/api/users/{userId}?search={search}" &&
                message.GetRequestMetadata()!.DependencyName == "RequestMetadataTest" &&
                message.GetRequestMetadata()!.RequestName == "GetUser"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sutClient.GetUser("myUser", "searchParam");

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task ClientDependencyMethodNameAsync()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users" &&
                message.GetRequestMetadata()!.RequestRoute == "/api/users" &&
                message.GetRequestMetadata()!.DependencyName == "RequestMetadataTest" &&
                message.GetRequestMetadata()!.RequestName == "GetUsers"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sutClient.GetUsersAsync();

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task ApiDependencyMethodNameAsync()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users" &&
                message.GetRequestMetadata()!.RequestRoute == "/api/users" &&
                message.GetRequestMetadata()!.DependencyName == "RequestMetadataTest" &&
                message.GetRequestMetadata()!.RequestName == "GetUsers"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sutApi.GetUsersAsync();

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task CustomDependencyName()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/user" &&
                message.GetRequestMetadata()!.RequestRoute == "/api/user" &&
                message.GetRequestMetadata()!.DependencyName == "MyDependency" &&
                message.GetRequestMetadata()!.RequestName == "GetUser"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sutCustom.GetUser();

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task CustomRequestName()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users" &&
                message.GetRequestMetadata()!.RequestRoute == "/api/users" &&
                message.GetRequestMetadata()!.DependencyName == "MyDependency" &&
                message.GetRequestMetadata()!.RequestName == "MyRequestName"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sutCustom.GetUsers();

        Assert.Equal("Success!", response);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _provider.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
