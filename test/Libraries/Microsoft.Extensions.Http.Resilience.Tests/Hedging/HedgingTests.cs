// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Internal.Routing;
using Microsoft.Extensions.Telemetry.Metering;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging;

#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

public abstract class HedgingTests<TBuilder> : IDisposable
{
    public const string ClientId = "clientId";

    public const int DefaultHedgingAttempts = 3;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Mock<IRequestClonerInternal> _requestCloneHandlerMock;
    private readonly Mock<IRequestRoutingStrategy> _requestRoutingStrategyMock;
    private readonly Mock<IPooledRequestRoutingStrategyFactory> _requestRoutingStrategyFactoryMock;
    private readonly IServiceCollection _services;
    private readonly List<string> _requests = new();
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly Func<IHttpClientBuilder, IRequestRoutingStrategyFactory, TBuilder> _createDefaultBuilder;
    private bool _failure;

    protected HedgingTests(Func<IHttpClientBuilder, IRequestRoutingStrategyFactory, TBuilder> createDefaultBuilder)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _requestCloneHandlerMock = new Mock<IRequestClonerInternal>(MockBehavior.Strict);
        _requestRoutingStrategyMock = new Mock<IRequestRoutingStrategy>(MockBehavior.Strict);
        _requestRoutingStrategyFactoryMock = new Mock<IPooledRequestRoutingStrategyFactory>(MockBehavior.Strict);

        _services = new ServiceCollection().RegisterMetering().AddLogging();
        _services.AddSingleton(_requestCloneHandlerMock.Object);
        _services.AddSingleton<IRedactorProvider>(NullRedactorProvider.Instance);

        var httpClient = _services.AddHttpClient(ClientId);

        Builder = createDefaultBuilder(httpClient, _requestRoutingStrategyFactoryMock.Object);
        _ = httpClient.AddHttpMessageHandler(() => new TestHandlerStub(InnerHandlerFunction));
        _createDefaultBuilder = createDefaultBuilder;
    }

    public TBuilder Builder { get; private set; }

    public void Dispose()
    {
        _requestCloneHandlerMock.VerifyAll();
        _requestRoutingStrategyMock.VerifyAll();
        _requestRoutingStrategyFactoryMock.VerifyAll();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    [Fact]
    public void AddHedging_EnsureRequestCloner()
    {
        var services = new ServiceCollection();

        _createDefaultBuilder(services.AddHttpClient("dummy"), _requestRoutingStrategyFactoryMock.Object);

        Assert.NotNull(services.BuildServiceProvider().GetRequiredService<IRequestClonerInternal>());
    }

    [Fact]
    public async Task SendAsync_EnsureContextFlows()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");
        var context = new Context { ["custom-data"] = "my-data" };
        request.SetPolicyExecutionContext(context);
        var calls = 0;

        SetupRouting();
        SetupRoutes(3, "https://enpoint-{0}:80");
        _services.RemoveAll<IRequestClonerInternal>();
        _services.AddRequestCloner();
        ConfigureHedgingOptions(options =>
        {
            options.OnHedgingAsync = args =>
            {
                Assert.Equal("my-data", (string)args.Context["custom-data"]);
                calls++;
                return Task.CompletedTask;
            };
        });

        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);

        using var client = CreateClientWithHandler();

        await client.SendAsync(request, _cancellationTokenSource.Token);

        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task SendAsync_NoErrors_ShouldReturnSingleResponse()
    {
        SetupRouting();
        SetupRoutes(1, "https://enpoint-{0}:80/");
        using var client = CreateClientWithHandler();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");
        SetupCloner(request, false);

        AddResponse(HttpStatusCode.OK);

        var response = await client.SendAsync(request, _cancellationTokenSource.Token);
        Assert.Empty(_responses);

        Assert.Single(_requests);
        Assert.Equal("https://enpoint-1:80/some-path?query", _requests[0]);
    }

    [Fact]
    public async Task SendAsync_NoRoutes_Throws()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");

        SetupRouting(false);
        SetupRoutes(0);

        _failure = true;

        using var client = CreateClientWithHandler();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.SendAsync(request, _cancellationTokenSource.Token));
        Assert.Equal("The routing strategy did not provide any route URL on the first attempt.", exception.Message);
    }

    [Fact]
    public async Task SendAsync_NoRoutesLeftAndNoResult_ShouldThrow()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");

        SetupRouting();
        SetupCloner(request, true);
        SetupRoutes(2);

        _failure = true;

        using var client = CreateClientWithHandler();

        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await client.SendAsync(request, _cancellationTokenSource.Token));
        Assert.Equal("Something went wrong!", exception.Message);

        Assert.Equal(2, _requests.Count);
        Assert.Equal(2, _requests.Distinct().Count());
    }

    [Fact]
    public async Task SendAsync_NoRoutesLeftAndSomeResultPresent_ShouldReturn()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");

        SetupRouting();
        SetupCloner(request, true);
        SetupRoutes(4);

        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.ServiceUnavailable);

        using var client = CreateClientWithHandler();

        var result = await client.SendAsync(request, _cancellationTokenSource.Token);
        Assert.Equal(DefaultHedgingAttempts, _requests.Count);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
    }

    [Fact]
    public async Task SendAsync_NoRoutesLeft_EnsureLessThanMaxHedgedAttempts()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");

        SetupRouting();
        SetupCloner(request, true);
        SetupRoutes(2);

        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);

        using var client = CreateClientWithHandler();

        var result = await client.SendAsync(request, _cancellationTokenSource.Token);
        Assert.Equal(2, _requests.Count);

        _requestCloneHandlerMock.Verify(o => o.CreateSnapshot(It.IsAny<HttpRequestMessage>()), Times.Exactly(1));
    }

    [Fact]
    public async Task SendAsync_FailedExecution_ShouldReturnResponseFromHedging()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");

        SetupRouting();
        SetupCloner(request, true);
        SetupRoutes(3, "https://enpoint-{0}:80");

        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.ServiceUnavailable);
        AddResponse(HttpStatusCode.OK);

        using var client = CreateClientWithHandler();

        var result = await client.SendAsync(request, _cancellationTokenSource.Token);
        Assert.Equal(3, _requests.Count);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("https://enpoint-1:80/some-path?query", _requests[0]);
        Assert.Equal("https://enpoint-2:80/some-path?query", _requests[1]);
        Assert.Equal("https://enpoint-3:80/some-path?query", _requests[2]);
    }

    protected void AddResponse(HttpStatusCode statusCode)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        _responses.Enqueue(new HttpResponseMessage(statusCode));
#pragma warning restore CA2000 // Dispose objects before losing scope
    }

    protected abstract void ConfigureHedgingOptions(Action<HttpHedgingPolicyOptions> configure);

    protected System.Net.Http.HttpClient CreateClientWithHandler() => _services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient(ClientId);

    protected void SetupCloner(HttpRequestMessage request, bool createCalled)
    {
        var snapshot = createCalled ?
            Mock.Of<IHttpRequestMessageSnapshot>(v => v.Create() == request) :
            Mock.Of<IHttpRequestMessageSnapshot>();

        _requestCloneHandlerMock
            .Setup(mock => mock.CreateSnapshot(It.IsAny<HttpRequestMessage>()))
            .Returns(snapshot);
    }

    private Task<HttpResponseMessage> InnerHandlerFunction(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request.RequestUri!.ToString());

        if (_failure)
        {
            throw new HttpRequestException("Something went wrong!");
        }

        return Task.FromResult(_responses.Dequeue());
    }

    private void SetupRoutes(int totalAttempts, string pattern = "https://dummy-{0}")
    {
        int attemptCount = 0;

        Uri? outUri = null;
        _requestRoutingStrategyMock
            .Setup(mock => mock.TryGetNextRoute(out outUri))
            .Callback((out Uri? uri) =>
            {
                attemptCount++;
                uri = new Uri(string.Format(CultureInfo.InvariantCulture, pattern, attemptCount));
            })
            .Returns(() => attemptCount <= totalAttempts);
    }

    private void SetupRouting(bool mustReturn = true)
    {
        _requestRoutingStrategyFactoryMock.Setup(s => s.CreateRoutingStrategy()).Returns(() => _requestRoutingStrategyMock.Object);
        if (mustReturn)
        {
            _requestRoutingStrategyFactoryMock.Setup(s => s.ReturnRoutingStrategy(_requestRoutingStrategyMock.Object)).Verifiable();
        }
    }
}
