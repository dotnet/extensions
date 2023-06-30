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
    NoRoutes = 1 << 3,
}

internal static class HttpClientFactory
{
    internal const string EmptyClient = "Empty";
    internal const string StandardClient = "Standard";
    internal const string SingleHandlerClient = "SingleHandler";
    internal const string PrimaryEndpoint = "http://localhost1";
    internal const string SecondaryEndpoint = "http://localhost2";

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
            .ConfigurePrimaryHttpMessageHandler(() => new NoRemoteCallHandler())
            .Services
            .AddHttpClient(EmptyClient, client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new NoRemoteCallHandler())
            .Services
            .AddHttpClient(SingleHandlerClient, client => client.Timeout = Timeout.InfiniteTimeSpan)
            .AddHttpMessageHandler(() => new EmptyHandler())
            .ConfigurePrimaryHttpMessageHandler(() => new NoRemoteCallHandler());

        services.RemoveAll<ILoggerFactory>();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        return services.BuildServiceProvider();
    }

    private static IServiceCollection AddHedging(this IServiceCollection services, HedgingClientType clientType)
    {
        var clientBuilder = services.AddHttpClient(clientType.ToString(), client => client.Timeout = Timeout.InfiniteTimeSpan);
        var hedgingBuilder = clientBuilder.AddStandardHedgingHandler().SelectStrategyByAuthority(SimpleClassifications.PublicData);
        _ = clientBuilder.AddHttpMessageHandler<NoRemoteCallHandler>();

        if (clientType.HasFlag(HedgingClientType.NoRoutes))
        {
            return services;
        }

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
                                Uri = new Uri(PrimaryEndpoint)
                            },
                            new WeightedEndpoint
                            {
                                Uri = new Uri(SecondaryEndpoint)
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
                                Uri = new Uri(PrimaryEndpoint)
                            },
                            new WeightedEndpoint
                            {
                                Uri = new Uri(SecondaryEndpoint)
                            }
                        }
                    };
                }).ToArray();
            });
        }

        return services;
    }
}
