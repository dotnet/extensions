// Assembly 'Microsoft.AspNetCore.Testing'

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Testing;

/// <summary>
/// Extension methods supporting Kestrel server unit testing scenarios.
/// </summary>
public static class ServiceFakesExtensions
{
    /// <summary>
    /// Adds an empty Startup class to satisfy ASP.NET check.
    /// </summary>
    /// <param name="builder">An <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> instance.</param>
    /// <returns>The same <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> instance to allow method chaining.</returns>
    public static IWebHostBuilder UseTestStartup(this IWebHostBuilder builder);

    /// <summary>
    /// Adds Kestrel server instance listening on the given HTTP port.
    /// </summary>
    /// <param name="builder">An <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> instance.</param>
    /// <returns>The same <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> instance to allow method chaining.</returns>
    /// <remarks>When a concrete port is set by caller, it's not further validated if the port is really free.</remarks>
    public static IWebHostBuilder ListenHttpOnAnyPort(this IWebHostBuilder builder);

    /// <summary>
    /// Adds Kestrel server instance listening on a random HTTPS port.
    /// </summary>
    /// <param name="builder">An <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> instance.</param>
    /// <param name="sslCertificate">An SSL certificate for the port. If null, a self-signed certificate is created and used.</param>
    /// <returns>The same <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> instance to allow method chaining.</returns>
    /// <remarks>When a concrete port is set by caller, it's not further validated if the port is really free.</remarks>
    public static IWebHostBuilder ListenHttpsOnAnyPort(this IWebHostBuilder builder, X509Certificate2? sslCertificate = null);

    /// <summary>
    /// Creates an <see cref="T:System.Net.Http.HttpClient" /> to call the hosted application.
    /// </summary>
    /// <param name="host">An <see cref="T:Microsoft.Extensions.Hosting.IHost" /> instance.</param>
    /// <param name="handler">The inner <see cref="T:System.Net.Http.HttpClientHandler" />.</param>
    /// <param name="addressFilter">Selects what address should be used. If null, takes the first available address.</param>
    /// <returns>An <see cref="T:System.Net.Http.HttpClient" /> configured to call the hosted application.</returns>
    public static HttpClient CreateClient(this IHost host, HttpMessageHandler? handler = null, Func<Uri, bool>? addressFilter = null);

    /// <summary>
    /// Gets the first available URI the server listens to that passes the filter.
    /// </summary>
    /// <param name="host">An <see cref="T:Microsoft.Extensions.Hosting.IHost" /> instance.</param>
    /// <returns>A <see cref="T:System.Uri" /> instance.</returns>
    public static IEnumerable<Uri> GetListenUris(this IHost host);
}
