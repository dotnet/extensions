// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Telemetry.Enrichment;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Enrichment.Test;

public class EnricherExtensionsTests
{
    private const string TestActivitySourceName = "testTraceSource";

    [Fact]
    public void AddTraceEnricher_GivenNullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddTraceEnricher<TestTraceEnricher>());

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddTraceEnricher(new TestTraceEnricher()));

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddTraceEnricher<TestTraceEnricher>());

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddTraceEnricher(new TestTraceEnricher()));

        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddOpenTelemetry().WithTracing(builder =>
                builder.AddTraceEnricher(null!)));
    }

    [Fact]
    public async Task TracerProviderBuilder_AddTraceEnricherT_AddsEnricherAndAppliesEnrichment()
    {
        using var activitySource = new ActivitySource(TestActivitySourceName);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(TestActivitySourceName)
                    .AddTraceEnricher<TestTraceEnricher>()))
            .StartAsync();

        var enrichmentProcessor = host.Services.GetRequiredService<TraceEnrichmentProcessor>();
        Assert.NotNull(enrichmentProcessor);

        var activity = activitySource.StartActivity("Test");
        activity?.AddTag("internalKey", "internalValue");
        activity?.Stop();
        await host.StopAsync();

        Assert.Equal("internalValue", activity?.GetTagItem("internalKey"));
        Assert.Equal(1, activity?.GetTagItem("enrichedKey"));
#if NETCOREAPP3_1_OR_GREATER
        Assert.Equal("enrichedValueOnStart", activity?.GetTagItem("enrichedKey_onStart"));
#endif
    }

    [Fact]
    public async Task IServiceCollection_AddTraceEnricherT_AddsEnricherAndAppliesEnrichment()
    {
        using var activitySource = new ActivitySource(TestActivitySourceName);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddTraceEnricher<TestTraceEnricher>()
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(TestActivitySourceName)))
            .StartAsync();

        var enrichmentProcessor = host.Services.GetRequiredService<TraceEnrichmentProcessor>();
        Assert.NotNull(enrichmentProcessor);

        var activity = activitySource.StartActivity("Test");
        activity?.AddTag("internalKey", "internalValue");
        activity?.Stop();
        await host.StopAsync();

        Assert.Equal("internalValue", activity?.GetTagItem("internalKey"));
        Assert.Equal(1, activity?.GetTagItem("enrichedKey"));
#if NETCOREAPP3_1_OR_GREATER
        Assert.Equal("enrichedValueOnStart", activity?.GetTagItem("enrichedKey_onStart"));
#endif
    }
#if NETCOREAPP3_1_OR_GREATER
    [Fact]
    public async Task TracerProviderBuilder_AddTraceOnStartEnricherT_AddsEnricherAndAppliesEnrichment()
    {
        using var activitySource = new ActivitySource(TestActivitySourceName);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(TestActivitySourceName)
                    .AddTraceEnricher<TestTraceOnStartEnricher>()))
            .StartAsync();

        var enrichmentProcessor = host.Services.GetRequiredService<TraceEnrichmentProcessor>();
        Assert.NotNull(enrichmentProcessor);

        var activity = activitySource.StartActivity("Test");
        activity?.AddTag("internalKey", "internalValue");
        activity?.Stop();
        await host.StopAsync();

        Assert.Equal("internalValue", activity?.GetTagItem("internalKey"));
        Assert.Equal("enrichedValueOnStart", activity?.GetTagItem("enrichedKey_onStart"));
    }

    [Fact]
    public async Task IServiceCollection_AddTraceOnStartEnricherT_AddsEnricherAndAppliesEnrichment()
    {
        using var activitySource = new ActivitySource(TestActivitySourceName);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddTraceEnricher<TestTraceOnStartEnricher>()
                    .AddSource(TestActivitySourceName)))
            .StartAsync();

        var enrichmentProcessor = host.Services.GetRequiredService<TraceEnrichmentProcessor>();
        Assert.NotNull(enrichmentProcessor);

        var activity = activitySource.StartActivity("Test");
        activity?.AddTag("internalKey", "internalValue");
        activity?.Stop();
        await host.StopAsync();

        Assert.Equal("internalValue", activity?.GetTagItem("internalKey"));
        Assert.Equal("enrichedValueOnStart", activity?.GetTagItem("enrichedKey_onStart"));
    }
#endif
    [Fact]
    public async Task TracerProviderBuilder_AddTraceEnricher_AddsEnricherAndAppliesEnrichment()
    {
        using var activitySource = new ActivitySource(TestActivitySourceName);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(TestActivitySourceName)
                    .AddTraceEnricher(new TestTraceEnricher())))
            .StartAsync();

        var enrichmentProcessor = host.Services.GetRequiredService<TraceEnrichmentProcessor>();
        Assert.NotNull(enrichmentProcessor);

        var activity = activitySource.StartActivity("Test");
        activity?.AddTag("internalKey", "internalValue");
        activity?.Stop();
        await host.StopAsync();

        Assert.Equal("internalValue", activity?.GetTagItem("internalKey"));
        Assert.Equal(1, activity?.GetTagItem("enrichedKey"));
#if NETCOREAPP3_1_OR_GREATER
        Assert.Equal("enrichedValueOnStart", activity?.GetTagItem("enrichedKey_onStart"));
#endif
    }

    [Fact]
    public async Task IServiceCollection_AddTraceEnricher_AddsEnricherAndAppliesEnrichment()
    {
        using var activitySource = new ActivitySource(TestActivitySourceName);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddTraceEnricher(new TestTraceEnricher())
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(TestActivitySourceName)))
            .StartAsync();

        var enrichmentProcessor = host.Services.GetRequiredService<TraceEnrichmentProcessor>();
        Assert.NotNull(enrichmentProcessor);

        var activity = activitySource.StartActivity("Test");
        activity?.AddTag("internalKey", "internalValue");
        activity?.Stop();
        await host.StopAsync();

        Assert.Equal("internalValue", activity?.GetTagItem("internalKey"));
        Assert.Equal(1, activity?.GetTagItem("enrichedKey"));
#if NETCOREAPP3_1_OR_GREATER
        Assert.Equal("enrichedValueOnStart", activity?.GetTagItem("enrichedKey_onStart"));
#endif
    }

    [Fact]
    public async Task TracerProviderBuilder_AddTraceEnricher_MultipleEnrichersShouldAddOnlyOneEnrichmentProcessor()
    {
        using ActivitySource activitySource = new ActivitySource(TestActivitySourceName);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(TestActivitySourceName)
                    .AddTraceEnricher<TestTraceEnricher>()
                    .AddTraceEnricher(new TestTraceEnricher2())))
            .StartAsync();

        var activity = activitySource.StartActivity("Test");
        activity?.AddTag("internalKey", "internalValue");
        activity?.Stop();
        await host.StopAsync();

        Assert.Equal("internalValue", activity?.GetTagItem("internalKey"));
        Assert.Equal(1, activity?.GetTagItem("enrichedKey"));
        Assert.Equal(1, activity?.GetTagItem("enrichedKey2"));
#if NETCOREAPP3_1_OR_GREATER
        Assert.Equal("enrichedValueOnStart", activity?.GetTagItem("enrichedKey_onStart"));
        Assert.Equal("enrichedValueOnStart2", activity?.GetTagItem("enrichedKey_onStart2"));
        int expectedTagsCount = 5;
#else
        int expectedTagsCount = 3;
#endif
        int actualTagsCount = 0;
        if (activity?.TagObjects != null)
        {
            foreach (var tg in activity.TagObjects)
            {
                actualTagsCount++;
            }
        }

        Assert.Equal(expectedTagsCount, actualTagsCount);
    }

    [Fact]
    public async Task IServiceCollection_AddTraceEnricher_MultipleEnrichersShouldAddOnlyOneEnrichmentProcessor()
    {
        using ActivitySource activitySource = new ActivitySource(TestActivitySourceName);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddTraceEnricher<TestTraceEnricher>()
                .AddTraceEnricher(new TestTraceEnricher2())
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddSource(TestActivitySourceName)))
            .StartAsync();

        var activity = activitySource.StartActivity("Test");
        activity?.AddTag("internalKey", "internalValue");
        activity?.Stop();
        await host.StopAsync();

        Assert.Equal("internalValue", activity?.GetTagItem("internalKey"));
        Assert.Equal(1, activity?.GetTagItem("enrichedKey"));
        Assert.Equal(1, activity?.GetTagItem("enrichedKey2"));
#if NETCOREAPP3_1_OR_GREATER
        Assert.Equal("enrichedValueOnStart", activity?.GetTagItem("enrichedKey_onStart"));
        Assert.Equal("enrichedValueOnStart2", activity?.GetTagItem("enrichedKey_onStart2"));
        int expectedTagsCount = 5;
#else
        int expectedTagsCount = 3;
#endif
        int actualTagsCount = 0;
        if (activity?.TagObjects != null)
        {
            foreach (var tg in activity.TagObjects)
            {
                actualTagsCount++;
            }
        }

        Assert.Equal(expectedTagsCount, actualTagsCount);
    }

    internal sealed class TestTraceEnricher : ITraceEnricher
    {
        public int TimesCalled { get; private set; }
        public void Enrich(Activity activity)
        {
            activity.SetTag("enrichedKey", ++TimesCalled);
        }

        public void EnrichOnActivityStart(Activity activity)
        {
            activity.SetTag("enrichedKey_onStart", "enrichedValueOnStart");
        }
    }

    internal sealed class TestTraceEnricher2 : ITraceEnricher
    {
        public int TimesCalled { get; private set; }
        public void Enrich(Activity activity)
        {
            activity.SetTag("enrichedKey2", ++TimesCalled);
        }

        public void EnrichOnActivityStart(Activity activity)
        {
            activity.SetTag("enrichedKey_onStart2", "enrichedValueOnStart2");
        }
    }

#if NETCOREAPP3_1_OR_GREATER
    internal sealed class TestTraceOnStartEnricher : ITraceEnricher
    {
        public void Enrich(Activity activity)
        {
            // no-op.
        }

        public void EnrichOnActivityStart(Activity activity)
        {
            activity.SetTag("enrichedKey_onStart", "enrichedValueOnStart");
        }
    }
#endif
}
