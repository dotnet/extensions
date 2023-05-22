// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
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
[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S1067:Expressions should not be too complex", Justification = "Mock conditions")]
public class HeadersTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock = new(MockBehavior.Strict);
    private readonly Mock<IHttpClientFactory> _factoryMock = new(MockBehavior.Strict);

    private readonly IStaticHeaderTestClient _sutStatic;
    private readonly IParamHeaderTestClient _sutParam;

    public HeadersTests()
    {
        _factoryMock.Setup(m => m.CreateClient("MyClient")).Returns(new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://example.com/")
        });

        var services = new ServiceCollection();
        services.AddSingleton(_ => _factoryMock.Object);
        services.AddStaticHeaderTestClient();
        services.AddParamHeaderTestClient();
        var provider = services.BuildServiceProvider();

        _sutStatic = provider.GetRequiredService<IStaticHeaderTestClient>();
        _sutParam = provider.GetRequiredService<IParamHeaderTestClient>();
    }

    [Fact]
    public async Task StaticHeader()
    {
        _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                    message.Method == HttpMethod.Get &&
                    message.RequestUri != null &&
                    message.RequestUri.PathAndQuery == "/api/users" &&
                    message.Headers.GetValues("X-MyHeader").First() == "MyValue"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Success!")
                });

        var response = await _sutStatic.GetUsers();

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task StaticHeaderMultipleInMethod()
    {
        _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                    message.Method == HttpMethod.Get &&
                    message.RequestUri != null &&
                    message.RequestUri.PathAndQuery == "/api/users" &&
                    message.Headers.GetValues("X-MyHeader").First() == "MyValue" &&
                    message.Headers.GetValues("X-MyHeader1").First() == "MyValue" &&
                    message.Headers.GetValues("X-MyHeader2").First() == "MyValue"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Success!")
                });

        var response = await _sutStatic.GetUsersHeaders();

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task HeaderFromParameter()
    {
        _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                    message.Method == HttpMethod.Get &&
                    message.RequestUri != null &&
                    message.RequestUri.PathAndQuery == "/api/users" &&
                    message.Headers.GetValues("X-MyHeader").First() == "MyParamValue"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Success!")
                });

        var response = await _sutParam.GetUsers("MyParamValue");

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task HeaderFromParameterCustomObject()
    {
        _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                    message.Method == HttpMethod.Get &&
                    message.RequestUri != null &&
                    message.RequestUri.PathAndQuery == "/api/users" &&
                    message.Headers.GetValues("X-MyHeader").First() == "CustomObjectToString"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Success!")
                });

        var response = await _sutParam.GetUsersObject(new IParamHeaderTestClient.CustomObject());

        Assert.Equal("Success!", response);
    }
}
