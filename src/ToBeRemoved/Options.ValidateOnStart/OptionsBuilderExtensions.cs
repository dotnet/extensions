// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Options.Validation;

/// <summary>
/// Extension methods for adding configuration related options services to the DI container via <see cref="OptionsBuilder{TOptions}"/>.
/// </summary>
public static class OptionsBuilderExtensions
{
    /// <summary>
    /// Adds named options that are automatically validated during startup using a built-in validator.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="name">Name of the options.</param>
    /// <typeparam name="TOptions">Options to validate.</typeparam>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
    /// <remarks>
    /// We recommend using custom generated validator.
    /// </remarks>
    public static OptionsBuilder<TOptions> AddValidatedOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(
        this IServiceCollection services,
        string? name = null)
        where TOptions : class
    {
        _ = Throw.IfNull(services);

        _ = services.AddOptions();

        return new OptionsBuilder<TOptions>(services, name ?? Options.DefaultName)
            .ValidateOnStart();
    }

    /// <summary>
    /// Adds named options that are automatically validated during startup using a custom validator.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="name">Name of the options.</param>
    /// <typeparam name="TOptions">Options to validate.</typeparam>
    /// <typeparam name="TValidateOptions">Validator to use.</typeparam>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
    public static OptionsBuilder<TOptions> AddValidatedOptions<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TValidateOptions>(
        this IServiceCollection services,
        string? name = null)
        where TOptions : class
        where TValidateOptions : class, IValidateOptions<TOptions>
    {
        _ = Throw.IfNull(services);

        services
            .AddOptions()
            .TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TOptions>, TValidateOptions>());

        return new OptionsBuilder<TOptions>(services, name ?? Options.DefaultName)
            .ValidateOnStart();
    }
}
