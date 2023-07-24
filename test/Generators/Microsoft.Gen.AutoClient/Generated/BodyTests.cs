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
[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S1067:Expressions should not be too complex", Justification = "Mock conditions")]
public class BodyTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handlerMock = new(MockBehavior.Strict);
    private readonly Mock<IHttpClientFactory> _factoryMock = new(MockBehavior.Strict);

    private readonly IBodyTestClient _sut;
    private readonly ServiceProvider _provider;

    public BodyTests()
    {
        _factoryMock.Setup(m => m.CreateClient("MyClient")).Returns(new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://example.com/")
        });

        var services = new ServiceCollection();
        services.AddSingleton(_ => _factoryMock.Object);
        services.AddBodyTestClient();
        _provider = services.BuildServiceProvider();

        _sut = _provider.GetRequiredService<IBodyTestClient>();
    }

    [Fact]
    public async Task JsonBody()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Post &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users" &&
                message.Content!.Headers.ContentType!.ToString() == "application/json; charset=utf-8" &&
                message.Content!.ReadAsStringAsync().Result == @"{""Value"":""MyBodyObjectValue""}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.PostUsers(new IBodyTestClient.BodyObject());

        Assert.Equal("Success!", response);
    }

    [Fact]
    public async Task TextBody()
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message =>
                message.Method == HttpMethod.Put &&
                message.RequestUri != null &&
                message.RequestUri.PathAndQuery == "/api/users" &&
                message.Content!.Headers.ContentType!.ToString() == "text/plain; charset=utf-8" &&
                message.Content!.ReadAsStringAsync().Result == "MyBodyObjectValue"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success!")
            });

        var response = await _sut.PutUsers(new IBodyTestClient.BodyObject());

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
