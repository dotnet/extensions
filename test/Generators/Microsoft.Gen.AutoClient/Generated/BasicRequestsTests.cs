// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.AutoClient;
using Moq;
using Moq.Protected;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.AutoClient.Test;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "IDisposable inside mock setups")]
public class BasicRequestsTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handlerMock = new(MockBehavior.Strict);
    private readonly Mock<IHttpClientFactory> _factoryMock = new(MockBehavior.Strict);
    private readonly IBasicTestClient _sut;
    private readonly ServiceProvider _provider;

    public BasicRequestsTests()
    {
        _factoryMock.Setup(m => m.CreateClient("MyClient")).Returns(new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://example.com/")
        });

        var services = new ServiceCollection();
        services.AddSingleton(_ => _factoryMock.Object);
        services.AddBasicTestClient();
        _provider = services.BuildServiceProvider();

        _sut = _provider.GetRequiredService<IBasicTestClient>();
    }

    [Fact]
    public async Task DeleteRequest()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Delete &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success DELETE!")
            });

        var response = await _sut.DeleteUsers();

        Assert.Equal("Success DELETE!", response);
    }

    [Fact]
    public async Task GetRequest()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success GET!")
            });

        var response = await _sut.GetUsers();

        Assert.Equal("Success GET!", response);
    }

    [Fact]
    public async Task GetRequestCancellationToken()
    {
        var ct = new CancellationToken(true);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success GET!")
            });

#if NET5_0_OR_GREATER
        await Assert.ThrowsAsync<TaskCanceledException>(() => _sut.GetUsersWithCancellationToken(ct));
#else
        await Assert.ThrowsAsync<OperationCanceledException>(() => _sut.GetUsersWithCancellationToken(ct));
#endif
    }

    [Fact]
    public async Task HeadRequest()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Head &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success HEAD!")
            });

        var response = await _sut.HeadUsers();

        Assert.Equal("Success HEAD!", response);
    }

    [Fact]
    public async Task OptionsRequest()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Options &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success OPTIONS!")
            });

        var response = await _sut.OptionsUsers();

        Assert.Equal("Success OPTIONS!", response);
    }

    [Fact]
    public async Task PatchRequest()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == new HttpMethod("PATCH") &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success PATCH!")
            });

        var response = await _sut.PatchUsers();

        Assert.Equal("Success PATCH!", response);
    }

    [Fact]
    public async Task PostRequest()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Post &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success POST!")
            });

        var response = await _sut.PostUsers();

        Assert.Equal("Success POST!", response);
    }

    [Fact]
    public async Task PutRequest()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Put &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success PUT!")
            });

        var response = await _sut.PutUsers();

        Assert.Equal("Success PUT!", response);
    }

    [Fact]
    public async Task UnsuccessfulResponse()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent("Forbidden")
            });

        var ex = await Assert.ThrowsAsync<AutoClientException>(() => _sut.GetUsers());
        Assert.Equal(403, ex.StatusCode);
        Assert.Equal("Forbidden", ex.HttpError!.RawContent);
        Assert.Equal("/api/users", ex.Path);
    }

    [Fact]
    public async Task FullUrlUsesBaseAddress()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.ToString() == "https://example.com/api/users"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Ok")
            });

        var response = await _sut.GetUsers();

        Assert.Equal("Ok", response);
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
