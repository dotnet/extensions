// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Holds parsed data for the HTTP host header.
/// </summary>
public readonly struct HostHeaderValue : IEquatable<HostHeaderValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostHeaderValue"/> struct.
    /// </summary>
    /// <param name="host">The address of the host.</param>
    /// <param name="port">The optional TCP port number on which the host is listening.</param>
    public HostHeaderValue(string host, int? port)
    {
        Host = Throw.IfNull(host);
        Port = port;
    }

    /// <summary>
    /// Gets the host address.
    /// </summary>
    /// <value>
    /// The address of the server.
    /// </value>
    public string Host { get; }

    /// <summary>
    /// Gets the port value.
    /// </summary>
    /// <value>
    /// The optional TCP port number on which the server is listening.
    /// </value>
    public int? Port { get; }

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="left">First value.</param>
    /// <param name="right">Second value.</param>
    /// <returns><see langword="true" /> if the operands are equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(HostHeaderValue left, HostHeaderValue right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">First value.</param>
    /// <param name="right">Second value.</param>
    /// <returns><see langword="true" /> if the operands are unequal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(HostHeaderValue left, HostHeaderValue right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Parses a host header value.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="result">The parsed result.</param>
    /// <returns><see langword="true"/> if the value was parsed successfully, <see langword="false"/> otherwise.</returns>
    public static bool TryParse(string value, [NotNullWhen(true)] out HostHeaderValue result)
    {
#pragma warning disable CA2234 // Pass system uri objects instead of strings
        var hs = HostString.FromUriComponent(value);
#pragma warning restore CA2234 // Pass system uri objects instead of strings

        var parsedHost = hs.Host;
        if (!string.IsNullOrEmpty(parsedHost))
        {
            result = new HostHeaderValue(parsedHost, hs.Port);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Determines whether this host header value and a specified host header value are identical.
    /// </summary>
    /// <param name="other">The other host header value.</param>
    /// <returns><see langword="true"/> if the two values are identical; otherwise, <see langword="false"/>.</returns>
    public bool Equals(HostHeaderValue other) => Host.Equals(other.Host, StringComparison.Ordinal) && Port == other.Port;

    /// <summary>
    /// Determines whether the specified object is equal to the current host header value.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> if the specified object is identical to the current host header value; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj) => obj is HostHeaderValue hostHeader && Equals(hostHeader);

    /// <summary>
    /// Gets a hash code for this object.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode() => HashCode.Combine(Host, Port);

    /// <summary>
    /// Gets a string representation of this object.
    /// </summary>
    /// <returns>A string representing this object.</returns>
    public override string ToString()
    {
        if (Port.HasValue)
        {
            return $"{Host}:{Port.Value}";
        }

        return Host;
    }
}
