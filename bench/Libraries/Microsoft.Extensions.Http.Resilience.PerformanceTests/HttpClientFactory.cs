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
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable EA0006 // Replace uses of 'Enum.GetName' and 'Enum.ToString' with the '[EnumStrings]' code generator for improved performance

namespace Microsoft.Extensions.Http.Resilience.Bench;

[Flags]
[SuppressMessage("Performance", "EA0004:Make types declared in an executable internal", Justification = "Needs to be public for BenchmarkDotNet consumption")]
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

    public static ServiceProvider InitializeServiceProvider(params HedgingClientType[] clientType)
    {
        var services = new ServiceCollection();
        services
            .RegisterMetrics()
            .AddSingleton<IRedactorProvider>(NullRedactorProvider.Instance)
            .AddTransient<NoRemoteCallHandler>()
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

        foreach (var type in clientType)
        {
            services.AddHedging(type);
        }

        return services.BuildServiceProvider();
    }

    private static void AddHedging(this IServiceCollection services, HedgingClientType clientType)
    {
        var clientBuilder = services.AddHttpClient(clientType.ToString(), client => client.Timeout = Timeout.InfiniteTimeSpan);
        var hedgingBuilder = clientBuilder.AddStandardHedgingHandler().SelectPipelineByAuthority(FakeClassifications.PublicData);
        _ = clientBuilder.AddHttpMessageHandler<NoRemoteCallHandler>();

        if (clientType.HasFlag(HedgingClientType.NoRoutes))
        {
            return;
        }

        int routes = clientType.HasFlag(HedgingClientType.ManyRoutes) ? 50 : 2;

        if (clientType.HasFlag(HedgingClientType.Ordered))
        {
            hedgingBuilder.RoutingStrategyBuilder.ConfigureOrderedGroups(options =>
            {
                options.Groups = Enumerable.Repeat(0, routes).Select(_ =>
                {
                    return new UriEndpointGroup
                    {
                        Endpoints = new[]
                        {
                            new WeightedUriEndpoint
                            {
                                Uri = new Uri(PrimaryEndpoint)
                            },
                            new WeightedUriEndpoint
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
                    return new WeightedUriEndpointGroup
                    {
                        Endpoints = new[]
                        {
                            new WeightedUriEndpoint
                            {
                                Uri = new Uri(PrimaryEndpoint)
                            },
                            new WeightedUriEndpoint
                            {
                                Uri = new Uri(SecondaryEndpoint)
                            }
                        }
                    };
                }).ToArray();
            });
        }
    }
}
