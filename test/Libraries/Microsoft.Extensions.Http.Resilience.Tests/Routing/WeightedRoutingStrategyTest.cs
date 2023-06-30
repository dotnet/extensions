// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.Http.Resilience.Routing.Internal.WeightedGroups;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Routing;

public class WeightedRoutingStrategyTest : RoutingStrategyTest
{
    private WeightedGroupSelectionMode _selectionMode;

    [Fact]
    public void GetRoutes_InitialAttempt_EnsureExpectedOutput()
    {
        _selectionMode = WeightedGroupSelectionMode.InitialAttempt;

        SetupRandomizer(0d);
        StrategyResultHelper("https://a/", "https://b/", "https://c/");

        SetupRandomizer(21d);
        StrategyResultHelper("https://b/", "https://a/", "https://c/");

        SetupRandomizer(51d);
        StrategyResultHelper("https://c/", "https://a/", "https://b/");
    }

    [Fact]
    public void GetRoutes_EveryAttempt_EnsureExpectedOutput()
    {
        _selectionMode = WeightedGroupSelectionMode.EveryAttempt;

        SetupRandomizer(0d);
        StrategyResultHelper("https://a/", "https://b/", "https://c/");
    }

    [Fact]
    public void Reload_Ok()
    {
        SetupRandomizer(1.0);

        ReloadHelper(
            (b, c) => b.ConfigureWeightedGroups(c.GetSection("section")),
            new()
            {
                { "section:groups:0:endpoints:0:uri", "https://a/" },
                { "section:groups:0:weight", "10" }
            },
            new()
            {
                { "section:groups:0:endpoints:0:uri", "https://b/" },
                { "section:groups:0:weight", "10" }
            },
            new[] { "https://a/" },
            new[] { "https://b/" });
    }

    protected override void Configure(IRoutingStrategyBuilder routingBuilder)
    {
        routingBuilder.ConfigureWeightedGroups(GetSection(new Dictionary<string, string>
        {
            { "groups:0:endpoints:0:uri", "https://a/" },
            { "groups:0:weight", "10" }
        }));

        routingBuilder.ConfigureWeightedGroups(options =>
        {
            options.SelectionMode = _selectionMode;

            var group = CreateGroup("https://b/");
            group.Weight = 20;

            var groups = new List<WeightedEndpointGroup>(options.Groups)
            {
                group
            };
            options.Groups = groups;
        });

        routingBuilder.ConfigureWeightedGroups((options, serviceProvider) =>
        {
            serviceProvider.Should().NotBeNull();
            var group = CreateGroup("https://c/");
            group.Weight = 30;
            options.Groups.Add(group);
        });
    }

    protected override IEnumerable<string> ConfigureMinRoutes(IRoutingStrategyBuilder routingBuilder)
    {
        routingBuilder.ConfigureWeightedGroups(options => options.Groups.Add(CreateGroup("https://dummy-route/")));
        yield return "https://dummy-route/";
    }

    protected override IEnumerable<Action<IRoutingStrategyBuilder>> ConfigureInvalidRoutes()
    {
        yield return builder => builder.ConfigureWeightedGroups(options => { });

        yield return builder => builder.ConfigureWeightedGroups(options =>
        {
            var group = CreateGroup("https://dummy");
            group.Weight = 0;

            var groups = new List<WeightedEndpointGroup>(options.Groups)
            {
                group
            };
            options.Groups = groups;
        });

        yield return builder => builder.ConfigureWeightedGroups(options =>
        {
            var group = CreateGroup("https://dummy");
            group.Weight = 99999;

            var groups = new List<WeightedEndpointGroup>(options.Groups)
            {
                group
            };
            options.Groups = groups;
        });

        yield return builder => builder.ConfigureWeightedGroups(options =>
        {
            var group = CreateGroup("https://dummy");
            group.Endpoints.Single().Weight = 0;

            var groups = new List<WeightedEndpointGroup>(options.Groups)
            {
                group
            };
            options.Groups = groups;
        });

        yield return builder => builder.ConfigureWeightedGroups(options =>
        {
            var group = CreateGroup("https://dummy");
            group.Endpoints.Single().Weight = 99999;

            var groups = new List<WeightedEndpointGroup>(options.Groups)
            {
                group
            };
            options.Groups = groups;
        });

        yield return builder => builder.ConfigureWeightedGroups(options =>
        {
            var groups = new List<WeightedEndpointGroup>(options.Groups)
            {
                null!
            };
            options.Groups = groups;
        });
    }

    internal override RequestRoutingStrategy CreateEmptyStrategy() => new WeightedGroupsRoutingStrategy(Mock.Of<Randomizer>(), Mock.Of<ObjectPool<WeightedGroupsRoutingStrategy>>());

    private static WeightedEndpointGroup CreateGroup(params string[] endpoints)
    {
        return CreateGroup(endpoints.Select(v => new WeightedEndpoint { Uri = new Uri(v) }).ToArray());
    }

    private static WeightedEndpointGroup CreateGroup(params WeightedEndpoint[] endpoint)
    {
        return new WeightedEndpointGroup
        {
            Endpoints = endpoint.ToList()
        };
    }
}
