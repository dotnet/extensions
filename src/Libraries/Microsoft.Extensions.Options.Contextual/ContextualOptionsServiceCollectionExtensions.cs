// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options.Contextual;
using Microsoft.Extensions.Options.Contextual.Internal;
using Microsoft.Extensions.Options.Contextual.Provider;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding contextual options services to the DI container.
/// </summary>
public static class ContextualOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds services required for using contextual options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddContextualOptions(this IServiceCollection services)
    {
        _ = Throw.IfNull(services).AddOptions();

        services.TryAdd(ServiceDescriptor.Singleton(typeof(IContextualOptionsFactory<>), typeof(ContextualOptionsFactory<>)));
        services.TryAdd(ServiceDescriptor.Singleton(typeof(IContextualOptions<,>), typeof(ContextualOptions<,>)));
        services.TryAdd(ServiceDescriptor.Singleton(typeof(INamedContextualOptions<,>), typeof(ContextualOptions<,>)));

        return services;
    }

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="loadOptions">The action used to configure the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection Configure<TOptions>(
        this IServiceCollection services,
        Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<TOptions>>> loadOptions)
        where TOptions : class
        => services.Configure(Options.Options.DefaultName, Throw.IfNull(loadOptions));

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="name">The name of the options to configure.</param>
    /// <param name="loadOptions">The action used to configure the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection Configure<TOptions>(
        this IServiceCollection services,
        string? name,
        Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<TOptions>>> loadOptions)
        where TOptions : class
        => services
            .AddContextualOptions()
            .AddSingleton<ILoadContextualOptions<TOptions>>(
                new LoadContextualOptions<TOptions>(
                    name,
                    Throw.IfNull(loadOptions)));

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">The action used to configure the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
#pragma warning disable S3872 // Parameter names should not duplicate the names of their methods
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, Action<IOptionsContext, TOptions> configure)
#pragma warning restore S3872 // Parameter names should not duplicate the names of their methods
        where TOptions : class
        => services.Configure(Options.Options.DefaultName, Throw.IfNull(configure));

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="name">The name of the options to configure.</param>
    /// <param name="configure">The action used to configure the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
#pragma warning disable S3872 // Parameter names should not duplicate the names of their methods
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string? name, Action<IOptionsContext, TOptions> configure)
#pragma warning restore S3872 // Parameter names should not duplicate the names of their methods
        where TOptions : class
    {
        return services.AddContextualOptions().AddSingleton<ILoadContextualOptions<TOptions>>(
            new LoadContextualOptions<TOptions>(
                Throw.IfNull(name),
                (context, _) =>
                    new ValueTask<IConfigureContextualOptions<TOptions>>(
                        new ConfigureContextualOptions<TOptions>(Throw.IfNull(configure), Throw.IfNull(context)))));
    }

    /// <summary>
    /// Registers an action used to configure all instances of a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="loadOptions">The action used to configure the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection ConfigureAll<TOptions>(
        this IServiceCollection services,
        Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<TOptions>>> loadOptions)
        where TOptions : class
        => services.Configure(name: null, Throw.IfNull(loadOptions));

    /// <summary>
    /// Registers an action used to configure all instances of a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">The action used to configure the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection ConfigureAll<TOptions>(this IServiceCollection services, Action<IOptionsContext, TOptions> configure)
        where TOptions : class
        => services.Configure(name: null, Throw.IfNull(configure));
}
