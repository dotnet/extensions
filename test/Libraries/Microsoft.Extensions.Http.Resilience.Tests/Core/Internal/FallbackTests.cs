// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internals;

public sealed class FallbackTests : IDisposable
{
    private const string ClientName = "fallback";
    private static readonly Uri _fallbackBaseUri = new("https://www.example-fallback.com");
    private readonly IHttpClientBuilder _clientBuilder;
    private readonly Mock<IRequestClonerInternal> _requestCloneHandlerMock;
    private readonly List<Uri> _requests = new();
    private bool _fail;

    public FallbackTests()
    {
        _requestCloneHandlerMock = new Mock<IRequestClonerInternal>(MockBehavior.Strict);

        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddSingleton(_requestCloneHandlerMock.Object);

        _clientBuilder = services
            .AddHttpClient(ClientName)
            .AddFallbackHandler(options => options.BaseFallbackUri = _fallbackBaseUri);
    }

    public void Dispose()
    {
        _requestCloneHandlerMock.VerifyAll();
    }

    [Fact]
    public async Task SendAsync_SuccessfullExecution_ShouldReturnResponseWithoutFallback()
    {
        var client = CreateClientWithHandler();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.dummy-request.com/abc");
        var response = await client.SendAsync(request, default);

        Assert.Single(_requests);
        Assert.Equal("https://www.dummy-request.com/abc", _requests[0].ToString());
    }

    [Fact]
    public async Task SendAsync_FailedExecution_ShouldReturnResponseFromFallback()
    {
        var client = CreateClientWithHandler();

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.dummy-request.com/abc?x=x");
        _fail = true;
        _requestCloneHandlerMock.Setup(mock => mock.CreateSnapshot(request)).Returns(Mock.Of<IHttpRequestMessageSnapshot>(v => v.Create() == request));
        var response = await client.SendAsync(request, default);

        Assert.Equal(2, _requests.Count);
        Assert.Equal("https://www.dummy-request.com/abc?x=x", _requests[0].ToString());
        Assert.Equal("https://www.example-fallback.com/abc?x=x", _requests[1].ToString());

    }

    private System.Net.Http.HttpClient CreateClientWithHandler()
    {
        _clientBuilder.AddHttpMessageHandler(() => new TestHandlerStub(InnerHandlerFunction));

        return _clientBuilder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient(ClientName);
    }

    private Task<HttpResponseMessage> InnerHandlerFunction(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request.RequestUri!);

        if (_fail)
        {
            _fail = false;
            throw new HttpRequestException("Something went wrong");
        }

        return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
    }
}
