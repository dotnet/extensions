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
using FluentAssertions;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.Http.Resilience.Test.Helpers;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging;

#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

public abstract class HedgingTests<TBuilder> : IDisposable
{
    public const string ClientId = "clientId";

    public const int DefaultHedgingAttempts = 2;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Mock<RequestRoutingStrategy> _requestRoutingStrategyMock;
    private readonly Func<RequestRoutingStrategy> _requestRoutingStrategyFactory;
    private readonly IServiceCollection _services;
    private readonly Queue<HttpResponseMessage> _responses = new();
    private bool _failure;

    private protected HedgingTests(Func<IHttpClientBuilder, Func<RequestRoutingStrategy>, TBuilder> createDefaultBuilder)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _requestRoutingStrategyMock = new Mock<RequestRoutingStrategy>(MockBehavior.Strict, new Randomizer());
        _requestRoutingStrategyFactory = () => _requestRoutingStrategyMock.Object;

        _services = new ServiceCollection().AddMetrics().AddLogging();
        _services.AddSingleton<IRedactorProvider>(NullRedactorProvider.Instance);

        var httpClient = _services.AddHttpClient(ClientId);

        Builder = createDefaultBuilder(httpClient, _requestRoutingStrategyFactory);
        _ = httpClient.AddHttpMessageHandler(() => new TestHandlerStub(InnerHandlerFunction));
    }

    public TBuilder Builder { get; private set; }

    public List<string> Requests { get; } = [];

    public List<ResilienceContext?> RequestContexts { get; } = [];

    public void Dispose()
    {
        _requestRoutingStrategyMock.VerifyAll();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    [Fact]
    public async Task SendAsync_EnsureContextFlows()
    {
        var key = new ResiliencePropertyKey<string>("custom-data");
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");
        var context = ResilienceContextPool.Shared.Get();
        context.Properties.Set(key, "my-data");
        request.SetResilienceContext(context);
        var calls = 0;

        SetupRouting();
        SetupRoutes(3, "https://enpoint-{0}:80");
        ConfigureHedgingOptions(options =>
        {
            options.OnHedging = args =>
            {
                args.ActionContext.Properties.GetValue(key, "").Should().Be("my-data");
                calls++;
                return default;
            };
        });

        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);

        using var client = CreateClientWithHandler();

        await client.SendAsync(request, _cancellationTokenSource.Token);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task SendAsync_NoErrors_ShouldReturnSingleResponse()
    {
        SetupRouting();
        SetupRoutes(1, "https://enpoint-{0}:80/");
        using var client = CreateClientWithHandler();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");

        AddResponse(HttpStatusCode.OK);

        var response = await client.SendAsync(request, _cancellationTokenSource.Token);
        AssertNoResponse();

        Assert.Single(Requests);
        Assert.Equal("https://enpoint-1:80/some-path?query", Requests[0]);
    }

    [Fact]
    public async Task SendAsync_NoRoutes_Throws()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");

        SetupRouting();
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
        SetupRoutes(2);

        _failure = true;

        using var client = CreateClientWithHandler();

        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await client.SendAsync(request, _cancellationTokenSource.Token));
        Assert.Equal("Something went wrong!", exception.Message);

        Assert.Equal(2, Requests.Count);
        Assert.Equal(2, Requests.Distinct().Count());
    }

    [Fact]
    public async Task SendAsync_NoRoutesLeftAndSomeResultPresent_ShouldReturn()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");

        SetupRouting();
        SetupRoutes(4);

        AddResponse(HttpStatusCode.ServiceUnavailable);
        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);

        using var client = CreateClientWithHandler();

        var result = await client.SendAsync(request, _cancellationTokenSource.Token);
        Assert.Equal(DefaultHedgingAttempts + 1, Requests.Count);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
    }

    [Fact]
    public async Task SendAsync_EnsureDistinctContextForEachAttempt()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");

        SetupRouting();
        SetupRoutes(4);

        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);

        using var client = CreateClientWithHandler();

        await client.SendAsync(request, _cancellationTokenSource.Token);

        RequestContexts.Distinct().OfType<ResilienceContext>().Should().HaveCount(3);
    }

    [Fact]
    public async Task SendAsync_EnsureContextReplacedInRequestMessage()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");
        var originalContext = ResilienceContextPool.Shared.Get();
        request.SetResilienceContext(originalContext);

        SetupRouting();
        SetupRoutes(4);

        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);

        using var client = CreateClientWithHandler();

        await client.SendAsync(request, _cancellationTokenSource.Token);

        RequestContexts.Distinct().OfType<ResilienceContext>().Should().HaveCount(3);

        request.GetResilienceContext().Should().Be(originalContext);
    }

    [Fact]
    public async Task SendAsync_NoRoutesLeft_EnsureLessThanMaxHedgedAttempts()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");

        SetupRouting();
        SetupRoutes(2);

        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.InternalServerError);

        using var client = CreateClientWithHandler();

        var result = await client.SendAsync(request, _cancellationTokenSource.Token);
        Assert.Equal(2, Requests.Count);
    }

    [Fact]
    public async Task SendAsync_FailedExecution_ShouldReturnResponseFromHedging()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");

        SetupRouting();
        SetupRoutes(3, "https://enpoint-{0}:80");

        AddResponse(HttpStatusCode.InternalServerError);
        AddResponse(HttpStatusCode.ServiceUnavailable);
        AddResponse(HttpStatusCode.OK);

        using var client = CreateClientWithHandler();

        var result = await client.SendAsync(request, _cancellationTokenSource.Token);
        Assert.Equal(3, Requests.Count);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("https://enpoint-1:80/some-path?query", Requests[0]);
        Assert.Equal("https://enpoint-2:80/some-path?query", Requests[1]);
        Assert.Equal("https://enpoint-3:80/some-path?query", Requests[2]);
    }

    [Fact]
    public async Task SendAsync_FailedConnect_ShouldReturnResponseFromHedging()
    {
        const string ClientName = "HedgingClient";

        // TODO: use mocked request destinations

        var services = new ServiceCollection();
        var clientBuilder = services
            .AddHttpClient(ClientName)
            .ConfigurePrimaryHttpMessageHandler(() =>
#if NETFRAMEWORK
		new WinHttpHandler
		{
			SendTimeout = TimeSpan.FromSeconds(1),
#else
                new SocketsHttpHandler
                {
                    ConnectTimeout = TimeSpan.FromSeconds(1),
                    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                    {
                        ClientCertificates = [],
                        RemoteCertificateValidationCallback = (_, _, _, _) => true
                    },
#endif
                })
            .AddStandardHedgingHandler(routing =>
                routing.ConfigureOrderedGroups(g =>
                {
                    g.Groups.Add(new UriEndpointGroup
                    {
                        Endpoints = [new WeightedUriEndpoint { Uri = new Uri("https://localhost:3000") }]
                    });

                    g.Groups.Add(new UriEndpointGroup
                    {
                        Endpoints = [new WeightedUriEndpoint { Uri = new Uri("https://microsoft.com") }]
                    });
                }))
            .Configure(opt =>
            {
                opt.Hedging.MaxHedgedAttempts = 10;
                opt.Hedging.Delay = TimeSpan.FromSeconds(11);
                opt.Endpoint.CircuitBreaker.FailureRatio = 0.99;
                opt.Endpoint.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(900);
                opt.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(200);
                opt.Endpoint.Timeout.Timeout = TimeSpan.FromSeconds(200);
            });

        using var provider = services.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
        using var client = clientFactory.CreateClient(ClientName);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var ex = await Record.ExceptionAsync(() => client.GetAsync("https://localhost:3000", cts.Token));
        Assert.Null(ex);
    }

    protected void AssertNoResponse() => Assert.Empty(_responses);

    protected void AddResponse(HttpStatusCode statusCode) => AddResponse(statusCode, 1);

    protected void AddResponse(HttpStatusCode statusCode, int count)
    {
        for (int i = 0; i < count; i++)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            _responses.Enqueue(new HttpResponseMessage(statusCode));
#pragma warning restore CA2000 // Dispose objects before losing scope
        }
    }

    protected abstract void ConfigureHedgingOptions(Action<HttpHedgingStrategyOptions> configure);

    protected HttpClient CreateClientWithHandler() => _services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient(ClientId);

    private Task<HttpResponseMessage> InnerHandlerFunction(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!.ToString());
        RequestContexts.Add(request.GetResilienceContext());

        if (_failure)
        {
            throw new HttpRequestException("Something went wrong!");
        }

        return Task.FromResult(_responses.Dequeue());
    }

    protected void SetupRoutes(int totalAttempts, string pattern = "https://dummy-{0}")
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

    protected void SetupRouting()
    {
        _requestRoutingStrategyMock.Setup(s => s.Dispose());
    }
}
