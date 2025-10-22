// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Represents an endpoint for a service.
/// </summary>
public abstract class ServiceEndpoint
{
    /// <summary>
    /// Gets the endpoint.
    /// </summary>
    public abstract EndPoint EndPoint { get; }

    /// <summary>
    /// Gets the collection of endpoint features.
    /// </summary>
    public abstract IFeatureCollection Features { get; }

    /// <summary>
    /// Creates a new <see cref="ServiceEndpoint"/>.
    /// </summary>
    /// <param name="endPoint">The endpoint being represented.</param>
    /// <param name="features">Features of the endpoint.</param>
    /// <returns>A newly initialized <see cref="ServiceEndpoint"/>.</returns>
    public static ServiceEndpoint Create(EndPoint endPoint, IFeatureCollection? features = null)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        return new ServiceEndpointImpl(endPoint, features);
    }

    /// <summary>
    /// Tries to convert a specified string representation to its <see cref="ServiceEndpoint"/> equivalent,
    /// and returns a value that indicates whether the conversion succeeded.
    /// </summary>
    /// <param name="value">A string that consists of an IP address or hostname, optionally followed by a colon and port number, or a URI.</param>
    /// <param name="serviceEndpoint">When this method returns, contains the equivalent <see cref="ServiceEndpoint"/> if the conversion succeeded; otherwise,
    /// <see langword="null"/>. This parameter is passed uninitialized; any value originally supplied will be overwritten.</param>
    /// <returns><see langword="true"/> if the string was successfully parsed into a <see cref="ServiceEndpoint"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value,
        [NotNullWhen(true)] out ServiceEndpoint? serviceEndpoint)
    {
        EndPoint? endPoint = TryParseEndPoint(value);

        if (endPoint != null)
        {
            serviceEndpoint = Create(endPoint);
            return true;
        }
        else
        {
            serviceEndpoint = null;
            return false;
        }
    }

    private static EndPoint? TryParseEndPoint(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
#pragma warning disable CS8602
            if (value.IndexOf("://", StringComparison.Ordinal) < 0 && Uri.TryCreate($"fakescheme://{value}", default, out var uri))
#pragma warning restore CS8602
            {
                var port = uri.Port > 0 ? uri.Port : 0;
                return IPAddress.TryParse(uri.Host, out var ip)
                    ? new IPEndPoint(ip, port)
                    : new DnsEndPoint(uri.Host, port);
            }

            if (Uri.TryCreate(value, default, out uri))
            {
                return new UriEndPoint(uri);
            }
        }

        return null;
    }
}
