// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods supporting Kestrel server unit testing scenarios.
/// </summary>
[Experimental(diagnosticId: Experiments.AspNetTesting, UrlFormat = Experiments.UrlFormat)]
public static class ServiceFakesHostExtensions
{
    private static readonly Func<Uri, bool> _defaultAddressFilter = static _ => true;

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
