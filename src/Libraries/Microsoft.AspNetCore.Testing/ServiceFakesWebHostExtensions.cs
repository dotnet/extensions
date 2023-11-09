// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extension methods supporting Kestrel server unit testing scenarios.
/// </summary>
public static class ServiceFakesWebHostExtensions
{
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
}
