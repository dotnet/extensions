﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging.Bench;

internal static class HttpClientFactory
{
    public static HttpClient CreateWithLoggingLogRequest(string fileName, int readLimit)
    {
        var services = new ServiceCollection();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DropMessageLoggerProvider>());

        return services
            .AddFakeRedaction()
            .AddSingleton(_ => NoRemoteCallHandler.Create(fileName))
            .AddHttpClientLogEnricher<BenchEnricher>()
            .AddHttpClient(nameof(fileName))
            .AddExtendedHttpClientLogging(options =>
            {
                options.BodySizeLimit = readLimit;
                options.RequestBodyContentTypes.Add(new("application/json"));
                options.RequestHeadersDataClasses.Add("Content-Type", FakeTaxonomy.PrivateData);
            })
            .AddHttpMessageHandler<NoRemoteCallHandler>()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(fileName));
    }

    public static HttpClient CreateWithLoggingLogResponse(string fileName, int readLimit)
    {
        var services = new ServiceCollection();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DropMessageLoggerProvider>());

        return services
            .AddFakeRedaction()
            .AddSingleton(_ => NoRemoteCallHandler.Create(fileName))
            .AddHttpClient(nameof(fileName))
            .AddExtendedHttpClientLogging(options =>
            {
                options.BodySizeLimit = readLimit;
                options.ResponseBodyContentTypes.Add(new("application/json"));
                options.ResponseHeadersDataClasses.Add("Content-Type", FakeTaxonomy.PrivateData);
            })
            .AddHttpMessageHandler<NoRemoteCallHandler>()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(fileName));
    }

    public static HttpClient CreateWithLoggingLogAll(string fileName, int readLimit)
    {
        var services = new ServiceCollection();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DropMessageLoggerProvider>());

        return services
            .AddFakeRedaction()
            .AddSingleton(_ => NoRemoteCallHandler.Create(fileName))
            .AddHttpClient(nameof(fileName))
            .AddExtendedHttpClientLogging(options =>
            {
                options.BodySizeLimit = readLimit;

                options.RequestBodyContentTypes.Add(new("application/json"));
                options.RequestHeadersDataClasses.Add("Content-Type", FakeTaxonomy.PrivateData);

                options.ResponseBodyContentTypes.Add(new("application/json"));
                options.ResponseHeadersDataClasses.Add("Content-Type", FakeTaxonomy.PrivateData);
            })
            .AddHttpMessageHandler<NoRemoteCallHandler>()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(fileName));
    }

    public static HttpClient CreateWithLoggingLogRequest_ChunkedEncoding(string fileName, int readLimit)
    {
        var services = new ServiceCollection();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DropMessageLoggerProvider>());

        return services
            .AddFakeRedaction()
            .AddSingleton(_ => NoRemoteCallNotSeekableHandler.Create(fileName))
            .AddHttpClient(nameof(fileName))
            .AddExtendedHttpClientLogging(options =>
            {
                options.BodySizeLimit = readLimit;
                options.RequestBodyContentTypes.Add("application/json");
                options.RequestHeadersDataClasses.Add("Content-Type", FakeTaxonomy.PrivateData);
            })
            .AddHttpMessageHandler<NoRemoteCallNotSeekableHandler>()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(fileName));
    }

    public static HttpClient CreateWithLoggingLogResponse_ChunkedEncoding(string fileName, int readLimit)
    {
        var services = new ServiceCollection();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DropMessageLoggerProvider>());

        return services
            .AddFakeRedaction()
            .AddSingleton(_ => NoRemoteCallNotSeekableHandler.Create(fileName))
            .AddHttpClient(nameof(fileName))
            .AddExtendedHttpClientLogging(options =>
            {
                options.BodySizeLimit = readLimit;
                options.ResponseBodyContentTypes.Add("application/json");
                options.ResponseHeadersDataClasses.Add("Content-Type", FakeTaxonomy.PrivateData);
            })
            .AddHttpMessageHandler<NoRemoteCallNotSeekableHandler>()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(fileName));
    }

    public static HttpClient CreateWithLoggingLogAll_ChunkedEncoding(string fileName, int readLimit)
    {
        var services = new ServiceCollection();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DropMessageLoggerProvider>());

        return services
            .AddFakeRedaction()
            .AddSingleton(_ => NoRemoteCallNotSeekableHandler.Create(fileName))
            .AddHttpClient(nameof(fileName))
            .AddExtendedHttpClientLogging(options =>
            {
                options.BodySizeLimit = readLimit;

                options.RequestBodyContentTypes.Add("application/json");
                options.RequestHeadersDataClasses.Add("Content-Type", FakeTaxonomy.PrivateData);

                options.ResponseBodyContentTypes.Add("application/json");
                options.ResponseHeadersDataClasses.Add("Content-Type", FakeTaxonomy.PrivateData);
            })
            .AddHttpMessageHandler<NoRemoteCallNotSeekableHandler>()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(fileName));
    }

    public static HttpClient CreateWithoutLogging(string fileName)
        => new ServiceCollection()
            .AddSingleton(_ => NoRemoteCallHandler.Create(fileName))
            .AddHttpClient(nameof(fileName))
            .AddHttpMessageHandler<NoRemoteCallHandler>()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(fileName));

    public static HttpClient CreateWithoutLogging_ChunkedEncoding(string fileName)
        => new ServiceCollection()
            .AddSingleton(_ => NoRemoteCallNotSeekableHandler.Create(fileName))
            .AddHttpClient(nameof(fileName))
            .AddHttpMessageHandler<NoRemoteCallNotSeekableHandler>()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(fileName));
}
