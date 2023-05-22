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

        return new OptionsBuilder<TOptions>(services, name ?? Microsoft.Extensions.Options.Options.DefaultName)
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

        return new OptionsBuilder<TOptions>(services, name ?? Microsoft.Extensions.Options.Options.DefaultName)
            .ValidateOnStart();
    }

#if !NET6_0_OR_GREATER
    /// <summary>
    /// Enforces options validation check in startup time rather then in runtime.
    /// </summary>
    /// <typeparam name="TOptions">Options to validate.</typeparam>
    /// <param name="optionsBuilder">The <see cref="OptionsBuilder{TOptions}"/> to configure options instance.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so that additional calls can be chained.</returns>
    private static OptionsBuilder<TOptions> ValidateOnStart<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(
        this OptionsBuilder<TOptions> optionsBuilder)
        where TOptions : class
    {
        _ = Throw.IfNull(optionsBuilder);

        var validatorSetter = new ValidatorSetter<TOptions>(optionsBuilder);

        // This will only add the hosted service once.
        _ = optionsBuilder.Services.AddHostedService<ValidationHostedService>()
            .AddOptions<ValidatorOptions>()
#pragma warning disable R9A034 // Optimize method group use to avoid allocations
            .Configure<IOptionsMonitor<TOptions>>(validatorSetter.SetValidator);
#pragma warning restore R9A034 // Optimize method group use to avoid allocations

        return optionsBuilder;
    }

    // This is a workaround. Originally it was implemented as a lambda expression in the ValidateOnStart<TOptions>
    // method above.
    // After trim analysis was enabled, compiler was not able to correctly propagate the DynamicallyAccessedMembers
    // attribute value of the TOptions generic parameter in ValidateOnStart<TOptions> into the lambda expression,
    // which resulted in trim analysis warnings (although there was no reason for them actually).
    internal sealed class ValidatorSetter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>
        where TOptions : class
    {
        private readonly OptionsBuilder<TOptions> _optionsBuilder;
        internal ValidatorSetter(OptionsBuilder<TOptions> optionsBuilder)
        {
            _optionsBuilder = optionsBuilder;
        }

        internal void SetValidator(ValidatorOptions vo, IOptionsMonitor<TOptions> options)
        {
            // This adds an action that resolves the options value to force evaluation
            // We don't care about the result as duplicates aren't important
            vo.Validators[(typeof(TOptions), _optionsBuilder.Name)] = () => options.Get(_optionsBuilder.Name);
        }
    }
#endif
}
