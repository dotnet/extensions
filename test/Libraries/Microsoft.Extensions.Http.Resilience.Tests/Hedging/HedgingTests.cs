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

    public List<string> Requests { get; } = new();

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

        Assert.Equal(3, calls);
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
