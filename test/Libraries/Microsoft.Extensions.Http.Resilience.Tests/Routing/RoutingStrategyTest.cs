// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Routing;

public abstract class RoutingStrategyTest
{
    public const string RoutingName = "dummy-routing";

    protected RoutingStrategyTest()
    {
        Builder = new RoutingStrategyBuilder(RoutingName, new ServiceCollection());
        Builder.Services.TryAddSingleton(Randomizer.Object);
    }

    public IRoutingStrategyBuilder Builder { get; set; }

    internal Mock<Randomizer> Randomizer { get; } = new Mock<Randomizer>(MockBehavior.Strict);

    public virtual bool CompareOrder => true;

    [Fact]
    public void Validate_Ok()
    {
        Configure(Builder);

        Assert.Throws<OptionsValidationException>(() => CreateStrategy("unknown"));
    }

    [Fact]
    public void CreateStrategy_EnsurePooled()
    {
        SetupRandomizer(60d);
        SetupRandomizer(60);
        Configure(Builder);

        var factory = CreateRoutingFactory();
        var strategies = new HashSet<RequestRoutingStrategy>();

        for (int i = 0; i < 10; i++)
        {
            using var strategy = factory();
            strategies.Add(strategy);
        }

        // assert that some strategies were pooled
        Assert.True(strategies.Count < 5);
    }

    [Fact]
    public virtual void MinRoutes_Ok()
    {
        SetupRandomizer(0);
        SetupRandomizer(0d);

        var routes = ConfigureMinRoutes(Builder).ToArray();

        var urls = CollectUrls(CreateStrategy()).ToArray();
        Assert.Equal(routes.Length, urls.Length);
        if (CompareOrder)
        {
            urls.Should().Equal(routes);
        }
        else
        {
            urls.Should().BeEquivalentTo(routes);
        }
    }

    [Fact]
    public void InvalidRoutes_ValidationException()
    {
        foreach (var action in ConfigureInvalidRoutes())
        {
            Builder = new RoutingStrategyBuilder(RoutingName, new ServiceCollection());
            Builder.Services.TryAddSingleton(Randomizer.Object);
            action(Builder);

            Assert.Throws<OptionsValidationException>(() => CreateStrategy());
        }
    }

    [Fact]
    public void TryGetNextRoute_NotInitialized_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => CreateEmptyStrategy().TryGetNextRoute(out _));
    }

    [Fact]
    public void TryGetNextRoute_AfterReset_Throws()
    {
        SetupRandomizer(0);

        Builder = new RoutingStrategyBuilder(RoutingName, new ServiceCollection());
        Builder.Services.TryAddSingleton(Randomizer.Object);
        Configure(Builder);

        var strategy = CreateStrategy();

        _ = ((IResettable)strategy).TryReset();

        Assert.Throws<InvalidOperationException>(() => strategy.TryGetNextRoute(out _));
    }

    protected void ReloadHelper(
        Action<IRoutingStrategyBuilder, IConfiguration> configure,
        Dictionary<string, string?> config1,
        Dictionary<string, string?> config2,
        string[] urls1,
        string[] urls2)
    {
        var provider = new ReloadableConfiguration();
        provider.Reload(config1);

        var builder = new ConfigurationBuilder();
        builder.Add(provider);
        configure(Builder, builder.Build());

        CollectUrls(CreateStrategy()).Should().Equal(urls1);

        // empty data -> failure
        Assert.Throws<AggregateException>(() => provider.Reload(new Dictionary<string, string?>()));

        provider.Reload(config2);
        CollectUrls(CreateStrategy()).Should().Equal(urls2);
    }

    internal void StrategyResultHelper(params string[] expectedUrls)
    {
        Builder = new RoutingStrategyBuilder(RoutingName, new ServiceCollection());
        Builder.Services.TryAddSingleton(Randomizer.Object);
        Configure(Builder);

        var factory = CreateRoutingFactory();

        CollectUrls(factory()).Should().Equal(expectedUrls);

        // intentionally, we check that the output on subsequent calls is the same
        CollectUrls(factory()).Should().Equal(expectedUrls);
    }

    internal RequestRoutingStrategy CreateStrategy(string? name = null) => CreateRoutingFactory(name)();

    internal Func<RequestRoutingStrategy> CreateRoutingFactory(string? name = null)
    {
        return Builder.Services
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<RequestRoutingStrategyOptions>>()
            .Get(name ?? Builder.Name).RoutingStrategyProvider!;
    }

    private static IEnumerable<string> CollectUrls(RequestRoutingStrategy strategy)
    {
        while (strategy.TryGetNextRoute(out var route))
        {
            yield return route.ToString();
        }
    }

    protected static IConfigurationSection GetSection(IDictionary<string, string> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>("section:" + pair.Key, pair.Value))).Build().GetSection("section");
    }

    protected abstract void Configure(IRoutingStrategyBuilder routingBuilder);

    protected abstract IEnumerable<string> ConfigureMinRoutes(IRoutingStrategyBuilder routingBuilder);

    protected abstract IEnumerable<Action<IRoutingStrategyBuilder>> ConfigureInvalidRoutes();

    internal abstract RequestRoutingStrategy CreateEmptyStrategy();

    protected void SetupRandomizer(double result) => Randomizer.Setup(r => r.NextDouble(It.IsAny<double>())).Returns(result);

    protected void SetupRandomizer(int result) => Randomizer.Setup(r => r.NextInt(It.IsAny<int>())).Returns(result);

    private class ReloadableConfiguration : ConfigurationProvider, IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return this;
        }

        public void Reload(Dictionary<string, string?> data)
        {
            Data = new Dictionary<string, string?>(data, StringComparer.OrdinalIgnoreCase);
            OnReload();
        }
    }
}
