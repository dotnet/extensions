// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Routing.Internal.OrderedGroups;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Routing;

public class OrderedRoutingStrategyTest : RoutingStrategyTest
{
    [Fact]
    public void GetRoutes_EnsureExpectedOutput()
    {
        Randomizer.Setup(r => r.NextDouble(10)).Returns(1);
        Randomizer.Setup(r => r.NextDouble(20)).Returns(2);
        Randomizer.Setup(r => r.NextDouble(30)).Returns(3);

        StrategyResultHelper("https://a/", "https://b/", "https://c/");

        Randomizer.VerifyAll();
    }

    [Fact]
    public void Reload_Ok()
    {
        SetupRandomizer(1.0);

        ReloadHelper(
            (b, c) => b.ConfigureOrderedGroups(c.GetSection("section")),
            new()
            {
                { "section:groups:0:endpoints:0:uri", "https://a/" },
            },
            new()
            {
                { "section:groups:0:endpoints:0:uri", "https://b/" },
            },
            new[] { "https://a/" },
            new[] { "https://b/" });
    }

    protected override void Configure(IRoutingStrategyBuilder routingBuilder)
    {
        routingBuilder.ConfigureOrderedGroups(GetSection(new Dictionary<string, string>
        {
            { "groups:0:endpoints:0:uri", "https://a/" },
            { "groups:0:endpoints:0:weight", "10" }
        }));

        routingBuilder.ConfigureOrderedGroups(options =>
        {
            var groups = new List<EndpointGroup>(options.Groups)
            {
                CreateGroup(new WeightedEndpoint { Uri = new Uri("https://b/"), Weight = 20 }),
            };
            options.Groups = groups;
        });

        routingBuilder.ConfigureOrderedGroups((options, serviceProvider) =>
        {
            serviceProvider.Should().NotBeNull();
            options.Groups.Add(CreateGroup(new WeightedEndpoint { Uri = new Uri("https://c/"), Weight = 30 }));
        });
    }

    protected override IEnumerable<string> ConfigureMinRoutes(IRoutingStrategyBuilder routingBuilder)
    {
        routingBuilder.ConfigureOrderedGroups(options => options.Groups.Add(CreateGroup("https://dummy-route/")));

        yield return "https://dummy-route/";
    }

    internal override IRequestRoutingStrategy CreateEmptyStrategy() => new OrderedGroupsRoutingStrategy(Mock.Of<IRandomizer>());

    protected override IEnumerable<Action<IRoutingStrategyBuilder>> ConfigureInvalidRoutes()
    {
        yield return builder => builder.ConfigureOrderedGroups(options => { });

        yield return builder => builder.ConfigureOrderedGroups(options =>
        {
            var group = CreateGroup("https://dummy");
            group.Endpoints.Single().Weight = 0;
            options.Groups.Add(group);
        });

        yield return builder => builder.ConfigureOrderedGroups(options =>
        {
            var group = CreateGroup("https://dummy");
            group.Endpoints.Single().Weight = 99999;
            options.Groups.Add(group);
        });

        yield return builder => builder.ConfigureOrderedGroups(options =>
        {
            options.Groups.Add(null!);
        });
    }

    private static EndpointGroup CreateGroup(params string[] endpoints)
    {
        return CreateGroup(endpoints.Select(v => new WeightedEndpoint { Uri = new Uri(v) }).ToArray());
    }

    private static EndpointGroup CreateGroup(params WeightedEndpoint[] endpoint)
    {
        return new EndpointGroup
        {
            Endpoints = endpoint.ToList()
        };
    }
}
