// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Telemetry.Metering;

#pragma warning disable R9A033 // Replace uses of 'Enum.GetName' and 'Enum.ToString' with the '[EnumStrings]' code generator for improved performance

namespace Microsoft.Extensions.Http.Resilience.Bench;

[Flags]
[SuppressMessage("Performance", "R9A031:Make types declared in an executable internal", Justification = "Needs to be public for BenchmarkDotNet consumption")]
public enum HedgingClientType
{
    Weighted = 1 << 0,
    Ordered = 1 << 1,
    ManyRoutes = 1 << 2,
}

internal static class HttpClientFactory
{
    internal const string EmptyClient = "Empty";

    internal const string StandardClient = "Standard";

    private const string HedgingEndpoint1 = "http://localhost1";
    private const string HedgingEndpoint2 = "http://localhost2";

    public static ServiceProvider InitializeServiceProvider(HedgingClientType clientType)
    {
        var services = new ServiceCollection();
        services
            .RegisterMetering()
            .AddSingleton<IRedactorProvider>(NullRedactorProvider.Instance)
            .AddTransient<NoRemoteCallHandler>()
            .AddHedging(clientType)
            .AddHttpClient(StandardClient, client => client.Timeout = Timeout.InfiniteTimeSpan)
            .AddStandardResilienceHandler()
            .Services
            .AddHttpClient(StandardClient)
            .AddHttpMessageHandler<NoRemoteCallHandler>()
            .Services
            .AddHttpClient(EmptyClient, client => client.Timeout = Timeout.InfiniteTimeSpan)
            .AddHttpMessageHandler<NoRemoteCallHandler>();

        services.RemoveAll<ILoggerFactory>();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        return services.BuildServiceProvider();
    }

    private static IServiceCollection AddHedging(this IServiceCollection services, HedgingClientType clientType)
    {
        var clientBuilder = services.AddHttpClient(clientType.ToString(), client => client.Timeout = Timeout.InfiniteTimeSpan);
        var hedgingBuilder = clientBuilder.AddStandardHedgingHandler().SelectPipelineByAuthority(SimpleClassifications.PublicData);
        _ = clientBuilder.AddHttpMessageHandler<NoRemoteCallHandler>();

        int routes = clientType.HasFlag(HedgingClientType.ManyRoutes) ? 50 : 2;

        if (clientType.HasFlag(HedgingClientType.Ordered))
        {
            hedgingBuilder.RoutingStrategyBuilder.ConfigureOrderedGroups(options =>
            {
                options.Groups = Enumerable.Repeat(0, routes).Select(_ =>
                {
                    return new EndpointGroup
                    {
                        Endpoints = new[]
                        {
                            new WeightedEndpoint
                            {
                                Uri = new Uri(HedgingEndpoint1)
                            },
                            new WeightedEndpoint
                            {
                                Uri = new Uri(HedgingEndpoint2)
                            }
                        }
                    };
                }).ToArray();
            });
        }
        else
        {
            hedgingBuilder.RoutingStrategyBuilder.ConfigureWeightedGroups(options =>
            {
                options.Groups = Enumerable.Repeat(0, routes).Select(_ =>
                {
                    return new WeightedEndpointGroup
                    {
                        Endpoints = new[]
                        {
                            new WeightedEndpoint
                            {
                                Uri = new Uri(HedgingEndpoint1)
                            },
                            new WeightedEndpoint
                            {
                                Uri = new Uri(HedgingEndpoint2)
                            }
                        }
                    };
                }).ToArray();
            });
        }

        return services;
    }
}
