// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.AutoClient.Test;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "IDisposable inside mock setups")]
public class PathTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handlerMock = new(MockBehavior.Strict);
    private readonly Mock<IHttpClientFactory> _factoryMock = new(MockBehavior.Strict);

    private readonly IPathTestClient _sut;
    private readonly ServiceProvider _provider;

    public PathTests()
    {
        _factoryMock.Setup(m => m.CreateClient("MyClient")).Returns(new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://example.com/")
        });

        var services = new ServiceCollection();
        services.AddSingleton(_ => _factoryMock.Object);
        services.AddPathTestClient();
        _provider = services.BuildServiceProvider();

        _sut = _provider.GetRequiredService<IPathTestClient>();
    }

    [Fact]
    public async Task SimplePath()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users/myUser"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.GetUser("myUser");

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task MultiplePathParameters()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users/myTenant/3"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.GetUserFromTenant("myTenant", 3);

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task EncodedPathParameters()
    {
        // "+=&" aren't required to be escaped in path segments but there is no harm in doing so

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users/some%2B%3D%26value/3"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.GetUserFromTenant("some+=&value", 3);

        Assert.Equal("Success!", response);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("a/b")]
    [InlineData("/b")]
    [InlineData("/b/")]
    [InlineData("b/")]
    [InlineData("//")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData(" .")]
    [InlineData(" ..")]
    [InlineData(". ")]
    [InlineData(".. ")]
    [InlineData(" . ")]
    [InlineData(" .. ")]
    [InlineData("../")]
    [InlineData("/..")]
    [InlineData("./..")]
    [InlineData("\\")]
    [InlineData("a\\b")]
    [InlineData("a\\")]
    [InlineData("\\b")]
    [InlineData("\\\\")]
    public async Task EncodedPathInvalidParameters(string value)
    {
        var encodedValue = Uri.EscapeDataString($"{value}");

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        var ex = await Assert.ThrowsAsync<ArgumentException>("tenantId", () => _sut.GetUserFromTenant(value, 3));
        Assert.Contains("The value can't contain '\\', '/', be empty, null or contain only dots (.).", ex.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("a..b")]
    [InlineData("..b")]
    [InlineData("a..")]
    [InlineData(" ..b")]
    [InlineData(" a..")]
    [InlineData("..b ")]
    [InlineData("a.. ")]
    [InlineData(" ..b ")]
    [InlineData(" a.. ")]
    public async Task EncodedPathValidParameters(string value)
    {
        var encodedValue = Uri.EscapeDataString($"{value}");

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.GetUserFromTenant(value, 3);
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
