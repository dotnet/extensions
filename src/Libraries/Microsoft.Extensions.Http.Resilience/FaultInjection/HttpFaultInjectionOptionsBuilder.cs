// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;
using Microsoft.Extensions.Resilience.FaultInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

/// <summary>
/// Builder class that inherits <see cref="FaultInjectionOptionsBuilder"/> to provide options configuration methods for
/// <see cref="FaultInjectionOptions"/>, <see cref="FaultInjectionExceptionOptions"/> and <see cref="HttpContentOptions"/>.
/// </summary>
public class HttpFaultInjectionOptionsBuilder
{
    private readonly FaultInjectionOptionsBuilder _faultInjectionOptionsBuilder;
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpFaultInjectionOptionsBuilder"/> class.
    /// </summary>
    /// <param name="services">The services collection.</param>
    public HttpFaultInjectionOptionsBuilder(IServiceCollection services)
    {
        _services = Throw.IfNull(services);
        _faultInjectionOptionsBuilder = new FaultInjectionOptionsBuilder(_services);
    }

    /// <summary>
    /// Configures default <see cref="FaultInjectionOptions"/>.
    /// </summary>
    /// <returns>
    /// The builder object itself so that additional calls can be chained.
    /// </returns>
    public HttpFaultInjectionOptionsBuilder Configure()
    {
        _ = _faultInjectionOptionsBuilder.Configure();
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
    public HttpFaultInjectionOptionsBuilder Configure(IConfiguration section)
    {
        _ = _faultInjectionOptionsBuilder.Configure(section);
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
    public HttpFaultInjectionOptionsBuilder Configure(Action<FaultInjectionOptions> configureOptions)
    {
        _ = _faultInjectionOptionsBuilder.Configure(configureOptions);
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
    public HttpFaultInjectionOptionsBuilder AddException(string key, Exception exception)
    {
        _ = _faultInjectionOptionsBuilder.AddException(key, exception);
        return this;
    }

    /// <summary>
    /// Adds a <see cref="HttpContentOptions"/> with the provided <see cref="HttpContent"/>.
    /// </summary>
    /// <param name="key">The identifier for the options instance to be added.</param>
    /// <param name="content">The http content.</param>
    /// <returns>The builder object itself so that additional calls can be chained.</returns>
    public HttpFaultInjectionOptionsBuilder AddHttpContent(string key, HttpContent content)
    {
        _ = Throw.IfNull(content);
        _ = Throw.IfNullOrWhitespace(key);

        _ = _services.Configure<HttpContentOptions>(key, o => o.HttpContent = content);

        return this;
    }
}
