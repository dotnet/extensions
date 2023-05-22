// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.HttpClient.SocketHandling;

/// <summary>
/// A builder for configuring named <see cref="SocketsHttpHandler"/> instances.
/// </summary>
public class SocketsHttpHandlerBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SocketsHttpHandlerBuilder"/> class.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    public SocketsHttpHandlerBuilder(IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        Name = builder.Name;
        Services = builder.Services;
    }

    /// <summary>
    /// Gets the name of <see cref="SocketsHttpHandler"/>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets services collection.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Adds a delegate that will execute the action on the primary handler.
    /// </summary>
    /// <param name="configure">The delegate to execute.</param>
    /// <returns>The <see cref="SocketsHttpHandlerBuilder"/>.</returns>
    public SocketsHttpHandlerBuilder ConfigureHandler(Action<SocketsHttpHandler> configure)
    {
        _ = Services.Configure<HttpClientFactoryOptions>(Name,
            options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(
                    item => configure((item.PrimaryHandler as SocketsHttpHandler)!));
            });

        return this;
    }

    /// <summary>
    /// Adds a delegate that will execute the action on the primary handler.
    /// </summary>
    /// <param name="configure">The delegate to execute.</param>
    /// <returns>The <see cref="SocketsHttpHandlerBuilder"/>.</returns>
    public SocketsHttpHandlerBuilder ConfigureHandler(Action<IServiceProvider, SocketsHttpHandler> configure)
    {
        _ = Services.Configure<HttpClientFactoryOptions>(
            Name, options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(item => configure(
                    item.Services,
                    (item.PrimaryHandler as SocketsHttpHandler)!));
            });

        return this;
    }

    /// <summary>
    /// Adds a delegate that will set <see cref="SocketsHttpHandler"/> as the primary <see cref="HttpMessageHandler"/>
    /// for a named <see cref="HttpClient"/> and will use <see cref="IConfigurationSection"/> to configure it.
    /// </summary>
    /// <param name="section">Configuration for <see cref="SocketsHttpHandlerOptions"/>.</param>
    /// <returns>The <see cref="SocketsHttpHandlerBuilder"/>.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(SocketsHttpHandlerOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public SocketsHttpHandlerBuilder ConfigureOptions(IConfigurationSection section)
    {
        _ = Throw.IfNull(section);

        _ = Services
            .Configure<SocketsHttpHandlerOptions>(Name, section)
            .AddValidatedOptions<SocketsHttpHandlerOptions, SocketsHttpHandlerOptionsValidator>();

        return this;
    }

    /// <summary>
    /// Adds a delegate that will set <see cref="SocketsHttpHandler"/> as the primary <see cref="HttpMessageHandler"/>
    /// for a named <see cref="HttpClient"/> and will use the delegate to configure it.
    /// </summary>
    /// <param name="configure">Configuration for <see cref="SocketsHttpHandlerOptions"/>.</param>
    /// <returns>The <see cref="SocketsHttpHandlerBuilder"/>.</returns>
    public SocketsHttpHandlerBuilder ConfigureOptions(Action<SocketsHttpHandlerOptions> configure)
    {
        _ = Throw.IfNull(configure);
        _ = Services.Configure(Name, configure);
        return this;
    }

    /// <summary>
    /// Disable verification of remote certificate on SSL/TLS connections.
    /// </summary>
    /// <returns>The <see cref="SocketsHttpHandlerBuilder"/>.</returns>
    [SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation", Justification = "Intentional")]
    public SocketsHttpHandlerBuilder DisableRemoteCertificateValidation()
    {
        return ConfigureHandler((_, handler) =>
        {
            handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        });
    }

    /// <summary>
    /// Adds a delegate to set a single client certificate for all remote endpoints.
    /// </summary>
    /// <param name="clientCertificate">The function to fetch the client certificate instance.</param>
    /// <returns>The <see cref="SocketsHttpHandlerBuilder"/>.</returns>
    public SocketsHttpHandlerBuilder ConfigureClientCertificate(Func<IServiceProvider, X509Certificate2> clientCertificate)
    {
        _ = Throw.IfNull(clientCertificate);

        return ConfigureHandler((provider, handler) =>
        {
            var x509Certificate2 = clientCertificate(provider);

            if (x509Certificate2 is null)
            {
                throw new InvalidDataException(
                    $"The parameter {nameof(clientCertificate)} returned null when called.");
            }

            handler.SslOptions.LocalCertificateSelectionCallback = (_, _, _, _, _) => x509Certificate2;
        });
    }
}
