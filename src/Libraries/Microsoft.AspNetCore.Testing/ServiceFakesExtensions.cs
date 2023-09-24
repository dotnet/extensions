// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Testing;

/// <summary>
/// Extension methods supporting Kestrel server unit testing scenarios.
/// </summary>
public static class ServiceFakesExtensions
{
    private static readonly Func<Uri, bool> _defaultAddressFilter = static _ => true;

    /// <summary>
    /// Adds an empty Startup class to satisfy ASP.NET check.
    /// </summary>
    /// <param name="builder">An <see cref="IWebHostBuilder"/> instance.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static IWebHostBuilder UseFakeStartup(this IWebHostBuilder builder)
    {
        return builder.UseStartup<FakeStartup>();
    }

    /// <summary>
    /// Adds Kestrel server instance listening on the given HTTP port.
    /// </summary>
    /// <param name="builder">An <see cref="IWebHostBuilder"/> instance.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <remarks>When a concrete port is set by caller, it's not further validated if the port is really free.</remarks>
    public static IWebHostBuilder ListenHttpOnAnyPort(this IWebHostBuilder builder)
        => Throw.IfNull(builder)
        .UseKestrel(options => options.Listen(new IPEndPoint(IPAddress.Loopback, 0)));

    /// <summary>
    /// Adds Kestrel server instance listening on a random HTTPS port.
    /// </summary>
    /// <param name="builder">An <see cref="IWebHostBuilder"/> instance.</param>
    /// <param name="sslCertificate">An SSL certificate for the port. If null, a self-signed certificate is created and used.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <remarks>When a concrete port is set by caller, it's not further validated if the port is really free.</remarks>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Dispose objects before losing scope")]
    public static IWebHostBuilder ListenHttpsOnAnyPort(this IWebHostBuilder builder, X509Certificate2? sslCertificate = null)
    {
        sslCertificate ??= FakeSslCertificateFactory.CreateSslCertificate();

        return builder
            .UseKestrel(options =>
            {
                options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                {
                    _ = listenOptions.UseHttps(sslCertificate);
                });
            })
            .ConfigureServices(services =>
                services.Configure<FakeCertificateOptions>(options =>
                    options.Certificate = sslCertificate));
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> to call the hosted application.
    /// </summary>
    /// <param name="host">An <see cref="IHost"/> instance.</param>
    /// <param name="handler">The inner <see cref="HttpClientHandler"/>.</param>
    /// <param name="addressFilter">Selects what address should be used. If null, takes the first available address.</param>
    /// <returns>An <see cref="HttpClient"/> configured to call the hosted application.</returns>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "not applicable")]
    [SuppressMessage("Reliability", "CA5399:HttpClient is created without enabling CheckCertificateRevocationList", Justification = "local calls")]
    public static HttpClient CreateClient(this IHost host, HttpMessageHandler? handler = null, Func<Uri, bool>? addressFilter = null)
    {
        _ = Throw.IfNull(host);
        addressFilter ??= _defaultAddressFilter;

        var uri = GetListenUris(host.Services.GetRequiredService<IServer>()).FirstOrDefault(addressFilter)
                  ?? throw new InvalidOperationException("No suitable address found to call the server.");

        if (handler is null)
        {
            var certificate = host.Services.GetService<IOptions<FakeCertificateOptions>>()?.Value.Certificate;
            if (certificate is not null)
            {
                var httpHandler = new FakeCertificateHttpClientHandler(certificate);
                return new HttpClient(httpHandler) { BaseAddress = uri };
            }

            return new HttpClient { BaseAddress = uri };
        }

        return new HttpClient(handler) { BaseAddress = uri };
    }

    /// <summary>
    /// Gets the first available URI the server listens to that passes the filter.
    /// </summary>
    /// <param name="host">An <see cref="IHost"/> instance.</param>
    /// <returns>A <see cref="Uri"/> instance.</returns>
    public static IEnumerable<Uri> GetListenUris(this IHost host)
    {
        return GetListenUris(Throw.IfNull(host).Services.GetRequiredService<IServer>());
    }

    private static IEnumerable<Uri> GetListenUris(IServer server)
    {
        var feature = server.Features.Get<IServerAddressesFeature>();

        // Stryker disable logical: we use the latter check to return static object instead of allocating a new one.
        if (feature is null || feature.Addresses.Count == 0)
        {
            return ArraySegment<Uri>.Empty;
        }

        return feature.Addresses
            .Select(x => new Uri(x.Replace("[::]", "localhost", StringComparison.OrdinalIgnoreCase)));
    }
}
