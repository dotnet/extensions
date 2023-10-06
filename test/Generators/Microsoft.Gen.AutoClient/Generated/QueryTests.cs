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
public class QueryTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handlerMock = new(MockBehavior.Strict);
    private readonly Mock<IHttpClientFactory> _factoryMock = new(MockBehavior.Strict);

    private readonly IQueryTestClient _sut;
    private readonly ServiceProvider _provider;

    public QueryTests()
    {
        _factoryMock.Setup(m => m.CreateClient("MyClient")).Returns(new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://example.com/")
        });

        var services = new ServiceCollection();
        services.AddSingleton(_ => _factoryMock.Object);
        services.AddQueryTestClient();
        _provider = services.BuildServiceProvider();

        _sut = _provider.GetRequiredService<IQueryTestClient>();
    }

    [Fact]
    public async Task QueryFromParameter()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users?paramQuery=myValue"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.GetUsers("myValue");

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task QueryIsEscaped()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users?paramQuery=http%3A%2F%2Fmicrosoft.com"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.GetUsers("http://microsoft.com");

        Assert.Equal("Success!", response);
    }

    [Theory]
    [InlineData("?")]
    [InlineData("=")]
    [InlineData("&")]
    [InlineData("%")]
    [InlineData("+")]
    [InlineData("#")]
    [InlineData(" ")]
    [InlineData("/")] // / isn't required to be escaped in query string values but there is no harm in doing so
    public async Task SensitiveCharsAreEscaped(string value)
    {
        var encodedValue = Uri.EscapeDataString(value);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == $"/api/users?paramQuery={encodedValue}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.GetUsers(value);

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task QueryFromParameterCustom()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users?paramQueryCustom=myValue"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.GetUsersCustom("myValue");

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task MultipleQueryParams()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users?paramQuery1=myValue1&paramQuery2=myValue2"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.GetUsers2("myValue1", "myValue2");

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task QueryParamCustomObject()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Get &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users?paramQuery=CustomObjectToString"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.GetUsersObject(new IQueryTestClient.CustomObject());

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
