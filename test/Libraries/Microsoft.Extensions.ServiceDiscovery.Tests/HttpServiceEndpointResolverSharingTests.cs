// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ServiceDiscovery.Http;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Tests;

public class HttpServiceEndpointResolverSharingTests
{
    [Fact]
    public void AddServiceDiscovery_HttpClient_SynchronousProviderDisposalDoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("test").AddServiceDiscovery();

        using var provider = services.BuildServiceProvider();
        using var handler = provider
            .GetRequiredService<IHttpMessageHandlerFactory>()
            .CreateHandler("test");

        Assert.NotNull(handler);
    }

    [Fact]
    public async Task AddServiceDiscoveryCore_RegistersResolverAsSingleton()
    {
        await using var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();

        var first = services.GetRequiredService<HttpServiceEndpointResolver>();
        var second = services.GetRequiredService<HttpServiceEndpointResolver>();

        Assert.Same(first, second);
    }

    [Fact]
    public async Task AddServiceDiscovery_HttpClient_HandlerUsesSharedResolverSingleton()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("test").AddServiceDiscovery();
        await using var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler("test");
        var resolvingHandler = FindHandler<ResolvingHttpDelegatingHandler>(handler);
        Assert.NotNull(resolvingHandler);

        var resolverField = typeof(ResolvingHttpDelegatingHandler)
            .GetField("_resolver", BindingFlags.Instance | BindingFlags.NonPublic);
        var usedResolver = resolverField!.GetValue(resolvingHandler);

        Assert.Same(provider.GetRequiredService<HttpServiceEndpointResolver>(), usedResolver);
    }

    [Fact]
    public async Task HandlerPipelines_ShareOneCleanupTimer_WhichStopsWithTheProvider()
    {
        var timeProvider = new TrackingTimeProvider();
        var services = new ServiceCollection()
            .AddSingleton<TimeProvider>(timeProvider)
            .AddSingleton<IServiceEndpointProviderFactory>(new FakeProviderFactory(started: null));
        services.AddHttpClient("first").ConfigurePrimaryHttpMessageHandler(() => new OkHttpMessageHandler()).AddServiceDiscovery();
        services.AddHttpClient("second").ConfigurePrimaryHttpMessageHandler(() => new OkHttpMessageHandler()).AddServiceDiscovery();
        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IHttpMessageHandlerFactory>();
        var forwarderFactory = provider.GetRequiredService<IServiceDiscoveryHttpMessageHandlerFactory>();
        HttpMessageHandler[] pipelines =
        [
            clientFactory.CreateHandler("first"),
            clientFactory.CreateHandler("second"),
            forwarderFactory.CreateHandler(new OkHttpMessageHandler()),
        ];
        foreach (var pipeline in pipelines)
        {
            using var invoker = new HttpMessageInvoker(pipeline, disposeHandler: false);
            using var request = new HttpRequestMessage(HttpMethod.Get, "http://basket/");
            using var response = await invoker.SendAsync(request, CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var cleanupTimer = Assert.Single(timeProvider.PeriodicTimers);
        Assert.False(cleanupTimer.IsDisposed);

        await provider.DisposeAsync();

        Assert.True(cleanupTimer.IsDisposed);
    }

    [Fact]
    public async Task SynchronousProviderDisposal_DuringResolution_CancelsTheResolution()
    {
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var services = new ServiceCollection()
            .AddSingleton<IServiceEndpointProviderFactory>(new FakeProviderFactory(started))
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var resolver = services.GetRequiredService<HttpServiceEndpointResolver>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "http://basket");
        var pending = resolver.GetEndpointAsync(request, CancellationToken.None).AsTask();
        await started.Task;

        services.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolutionAfterProviderDisposal_ThrowsWithoutRestartingTheCleanupTimer(bool disposeAsynchronously)
    {
        var timeProvider = new TrackingTimeProvider();
        var services = new ServiceCollection()
            .AddSingleton<TimeProvider>(timeProvider)
            .AddSingleton<IServiceEndpointProviderFactory>(new FakeProviderFactory(started: null))
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();
        var resolver = services.GetRequiredService<HttpServiceEndpointResolver>();

        if (disposeAsynchronously)
        {
            await services.DisposeAsync();
        }
        else
        {
            services.Dispose();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "http://basket");
        await Assert.ThrowsAsync<ObjectDisposedException>(() => resolver.GetEndpointAsync(request, CancellationToken.None).AsTask());
        Assert.Empty(timeProvider.PeriodicTimers);
    }

    private static T? FindHandler<T>(HttpMessageHandler handler)
        where T : HttpMessageHandler
    {
        for (var current = handler; current is not null; current = (current as DelegatingHandler)?.InnerHandler)
        {
            if (current is T match)
            {
                return match;
            }
        }

        return null;
    }

    private sealed class OkHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request });
    }

    /// <summary>
    /// Resolves every service to one loopback endpoint. When <paramref name="started"/> is supplied, resolution
    /// instead signals it and then waits until the watcher is disposed.
    /// </summary>
    private sealed class FakeProviderFactory(TaskCompletionSource<bool>? started) : IServiceEndpointProviderFactory, IServiceEndpointProvider
    {
#pragma warning disable CS0436 // Type conflicts with imported type
        public bool TryCreateProvider(ServiceEndpointQuery query, [NotNullWhen(true)] out IServiceEndpointProvider? provider)
#pragma warning restore CS0436 // Type conflicts with imported type
        {
            provider = this;
            return true;
        }

        public async ValueTask PopulateAsync(IServiceEndpointBuilder endpoints, CancellationToken cancellationToken)
        {
            if (started is null)
            {
                endpoints.Endpoints.Add(ServiceEndpoint.Create(new IPEndPoint(IPAddress.Loopback, 8080)));
                return;
            }

            _ = started.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        public ValueTask DisposeAsync() => default;
    }

    /// <summary>
    /// A <see cref="TimeProvider"/> whose timers never fire, and which records every timer it creates.
    /// </summary>
    private sealed class TrackingTimeProvider : TimeProvider
    {
        private readonly List<TrackingTimer> _timers = new();

        public IReadOnlyList<TrackingTimer> PeriodicTimers
        {
            get
            {
                lock (_timers)
                {
                    return _timers.Where(timer => timer.Period != TimeSpan.Zero && timer.Period != Timeout.InfiniteTimeSpan).ToList();
                }
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new TrackingTimer(period);
            lock (_timers)
            {
                _timers.Add(timer);
            }

            return timer;
        }
    }

    private sealed class TrackingTimer(TimeSpan period) : ITimer
    {
        public TimeSpan Period { get; } = period;

        public bool IsDisposed { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period) => !IsDisposed;

        public void Dispose() => IsDisposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}
