// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Extension methods for adding contextual options services to the DI container.
/// </summary>
public static class ContextualOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds services required for using contextual options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddContextualOptions(this IServiceCollection services)
    {
        _ = Throw.IfNull(services).AddOptions();

        services.TryAdd(ServiceDescriptor.Singleton(typeof(IContextualOptionsFactory<>), typeof(ContextualOptionsFactory<>)));
        services.TryAdd(ServiceDescriptor.Singleton(typeof(IContextualOptions<>), typeof(ContextualOptions<>)));
        services.TryAdd(ServiceDescriptor.Singleton(typeof(INamedContextualOptions<>), typeof(ContextualOptions<>)));

        return services;
    }

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="loadOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection Configure<TOptions>(
        this IServiceCollection services,
        Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<TOptions>>> loadOptions)
        where TOptions : class
        => services.Configure(Microsoft.Extensions.Options.Options.DefaultName, Throw.IfNull(loadOptions));

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="name">The name of the options to configure.</param>
    /// <param name="loadOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection Configure<TOptions>(
        this IServiceCollection services,
        string name,
        Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<TOptions>>> loadOptions)
        where TOptions : class
        => services
            .AddContextualOptions()
            .AddSingleton<ILoadContextualOptions<TOptions>>(
                new LoadContextualOptions<TOptions>(
                    Throw.IfNull(name),
                    Throw.IfNull(loadOptions)));

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, Action<IOptionsContext, TOptions> configureOptions)
        where TOptions : class
        => services.Configure(Microsoft.Extensions.Options.Options.DefaultName, Throw.IfNull(configureOptions));

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="name">The name of the options to configure.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, Action<IOptionsContext, TOptions> configureOptions)
        where TOptions : class
    {
        return services.AddContextualOptions().AddSingleton<ILoadContextualOptions<TOptions>>(
            new LoadContextualOptions<TOptions>(
                Throw.IfNull(name),
                (context, _) =>
                    new ValueTask<IConfigureContextualOptions<TOptions>>(
                        new ConfigureContextualOptions<TOptions>(Throw.IfNull(configureOptions), Throw.IfNull(context)))));
    }

    /// <summary>
    /// Registers an action used to initialize all instances of a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection PostConfigureAll<TOptions>(this IServiceCollection services, Action<IOptionsContext, TOptions> configureOptions)
        where TOptions : class
        => services.PostConfigure(null, Throw.IfNull(configureOptions));

    /// <summary>
    /// Registers an action used to initialize a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection PostConfigure<TOptions>(this IServiceCollection services, Action<IOptionsContext, TOptions> configureOptions)
        where TOptions : class
        => services.PostConfigure(Microsoft.Extensions.Options.Options.DefaultName, Throw.IfNull(configureOptions));

    /// <summary>
    /// Registers an action used to initialize a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection PostConfigure<TOptions>(this IServiceCollection services, string? name, Action<IOptionsContext, TOptions> configureOptions)
        where TOptions : class
        => services
            .AddContextualOptions()
            .AddSingleton<IPostConfigureContextualOptions<TOptions>>(
                new PostConfigureContextualOptions<TOptions>(name, Throw.IfNull(configureOptions)));

    /// <summary>
    /// Register a validation action for an options type.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be validated.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="validate">The validation function.</param>
    /// <param name="failureMessage">The failure message to use when validation fails.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection ValidateContextualOptions<TOptions>(this IServiceCollection services, Func<TOptions, bool> validate, string failureMessage)
        where TOptions : class
        => services.ValidateContextualOptions(Microsoft.Extensions.Options.Options.DefaultName, Throw.IfNull(validate), Throw.IfNull(failureMessage));

    /// <summary>
    /// Register a validation action for an options type.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be validated.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="validate">The validation function.</param>
    /// <param name="failureMessage">The failure message to use when validation fails.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection ValidateContextualOptions<TOptions>(this IServiceCollection services, string name, Func<TOptions, bool> validate, string failureMessage)
        where TOptions : class
        => services
            .AddContextualOptions()
            .AddSingleton<IValidateContextualOptions<TOptions>>(
                new ValidateContextualOptions<TOptions>(Throw.IfNull(name), Throw.IfNull(validate), Throw.IfNull(failureMessage)));
}
