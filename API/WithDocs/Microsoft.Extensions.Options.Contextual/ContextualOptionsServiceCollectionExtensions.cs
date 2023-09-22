// Assembly 'Microsoft.Extensions.Options.Contextual'

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Extension methods for adding contextual options services to the DI container.
/// </summary>
public static class ContextualOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds services required for using contextual options.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddContextualOptions(this IServiceCollection services);

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="loadOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<TOptions>>> loadOptions) where TOptions : class;

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="name">The name of the options to configure.</param>
    /// <param name="loadOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<TOptions>>> loadOptions) where TOptions : class;

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, Action<IOptionsContext, TOptions> configureOptions) where TOptions : class;

    /// <summary>
    /// Registers an action used to configure a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="name">The name of the options to configure.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, Action<IOptionsContext, TOptions> configureOptions) where TOptions : class;

    /// <summary>
    /// Registers an action used to initialize all instances of a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection PostConfigureAll<TOptions>(this IServiceCollection services, Action<IOptionsContext, TOptions> configureOptions) where TOptions : class;

    /// <summary>
    /// Registers an action used to initialize a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection PostConfigure<TOptions>(this IServiceCollection services, Action<IOptionsContext, TOptions> configureOptions) where TOptions : class;

    /// <summary>
    /// Registers an action used to initialize a particular type of options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be configured.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection PostConfigure<TOptions>(this IServiceCollection services, string? name, Action<IOptionsContext, TOptions> configureOptions) where TOptions : class;

    /// <summary>
    /// Register a validation action for an options type.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be validated.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="validate">The validation function.</param>
    /// <param name="failureMessage">The failure message to use when validation fails.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection ValidateContextualOptions<TOptions>(this IServiceCollection services, Func<TOptions, bool> validate, string failureMessage) where TOptions : class;

    /// <summary>
    /// Register a validation action for an options type.
    /// </summary>
    /// <typeparam name="TOptions">The options type to be validated.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="validate">The validation function.</param>
    /// <param name="failureMessage">The failure message to use when validation fails.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection ValidateContextualOptions<TOptions>(this IServiceCollection services, string name, Func<TOptions, bool> validate, string failureMessage) where TOptions : class;
}
