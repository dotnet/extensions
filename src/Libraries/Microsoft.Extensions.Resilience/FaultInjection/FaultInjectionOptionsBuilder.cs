// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Builder class to provide options configuration methods for
/// <see cref="FaultInjectionOptions"/> and <see cref="FaultInjectionExceptionOptions"/>.
/// </summary>
public class FaultInjectionOptionsBuilder
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="FaultInjectionOptionsBuilder"/> class.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    public FaultInjectionOptionsBuilder(IServiceCollection services)
    {
        _services = Throw.IfNull(services);
    }

    /// <summary>
    /// Configures default <see cref="FaultInjectionOptions"/>.
    /// </summary>
    /// <returns>
    /// The builder object itself so that additional calls can be chained.
    /// </returns>
    public FaultInjectionOptionsBuilder Configure()
    {
        _ = _services
            .AddValidatedOptions<FaultInjectionOptions, FaultInjectionOptionsValidator>();
        return this;
    }

    /// <summary>
    /// Configures <see cref="FaultInjectionOptions"/> through
    /// the provided <see cref="IConfigurationSection"/>.
    /// </summary>
    /// <param name="section">
    /// The configuration section to bind to <see cref="FaultInjectionOptions"/>.
    /// </param>
    /// <returns>The builder object itself so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(FaultInjectionOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public FaultInjectionOptionsBuilder Configure(IConfiguration section)
    {
        _ = Throw.IfNull(section);

        _ = _services
            .AddValidatedOptions<FaultInjectionOptions, FaultInjectionOptionsValidator>()
            .Bind(section);

        return this;
    }

    /// <summary>
    /// Configures <see cref="FaultInjectionOptions"/> through
    /// the provided configure.
    /// </summary>
    /// <param name="configureOptions">
    /// The function to be registered to configure <see cref="FaultInjectionOptions"/>.
    /// </param>
    /// <returns>The builder object itself so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Any parameter is <see langword="null"/>.
    /// </exception>
    public FaultInjectionOptionsBuilder Configure(Action<FaultInjectionOptions> configureOptions)
    {
        _ = Throw.IfNull(configureOptions);

        _ = _services
            .AddValidatedOptions<FaultInjectionOptions, FaultInjectionOptionsValidator>()
            .Configure(configureOptions);

        return this;
    }

    /// <summary>
    /// Add an exception instance to <see cref="FaultInjectionExceptionOptions"/>.
    /// </summary>
    /// <param name="key">The identifier for the exception instance to be added.</param>
    /// <param name="exception">The exception instance to be added.</param>
    /// <returns>The builder object itself so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is an empty string or <see langword="null"/>.
    /// </exception>
    public FaultInjectionOptionsBuilder AddException(string key, Exception exception)
    {
        _ = Throw.IfNull(exception);
        _ = Throw.IfNullOrWhitespace(key);

        _ = _services.Configure<FaultInjectionExceptionOptions>(key, o => o.Exception = exception);

        return this;
    }

    /// <summary>
    /// Add a custom result object instance to <see cref="FaultInjectionCustomResultOptions"/>.
    /// </summary>
    /// <param name="key">The identifier for the custom result object instance to be added.</param>
    /// <param name="customResult">The custom result object instance to be added.</param>
    /// <returns>The builder object itself so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="customResult"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is an empty string or <see langword="null"/>.
    /// </exception>
    [Experimental]
    public FaultInjectionOptionsBuilder AddCustomResult(string key, object customResult)
    {
        _ = Throw.IfNull(customResult);
        _ = Throw.IfNullOrWhitespace(key);

        _ = _services.Configure<FaultInjectionCustomResultOptions>(key, o => o.CustomResult = customResult);

        return this;
    }
}
