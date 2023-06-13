// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// Controls exception summarization.
/// </summary>
public static class ExceptionSummarizationExtensions
{
    /// <summary>
    /// Registers an exception summarizer into a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the summarizer to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddExceptionSummarizer(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddSingleton<IExceptionSummarizer, ExceptionSummarizer>();
        return services;
    }

    /// <summary>
    /// Registers an exception summarizer into a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the summarizer to.</param>
    /// <param name="configure">Delegates that configures the set of registered summary providers.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> are <see langword="null"/>.</exception>
    public static IServiceCollection AddExceptionSummarizer(this IServiceCollection services, Action<IExceptionSummarizationBuilder> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        services.TryAddSingleton<IExceptionSummarizer, ExceptionSummarizer>();
        configure(new ExceptionSummarizationBuilder(services));

        return services;
    }

    /// <summary>
    /// Registers a summary provider that handles <see cref="OperationCanceledException"/>, <see cref="WebException"/>, and <see cref="SocketException"/> .
    /// </summary>
    /// <param name="builder">The builder to attach the provider to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IExceptionSummarizationBuilder AddHttpProvider(this IExceptionSummarizationBuilder builder) => Throw.IfNull(builder).AddProvider<HttpExceptionSummaryProvider>();
}
