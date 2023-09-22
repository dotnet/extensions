// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Holds parsed data for the HTTP host header.
/// </summary>
public readonly struct HostHeaderValue : IEquatable<HostHeaderValue>
{
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
    /// Initializes a new instance of the <see cref="T:Microsoft.AspNetCore.HeaderParsing.HostHeaderValue" /> struct.
    /// </summary>
    /// <param name="host">The address of the host.</param>
    /// <param name="port">The optional TCP port number on which the host is listening.</param>
    public HostHeaderValue(string host, int? port);

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="left">First value.</param>
    /// <param name="right">Second value.</param>
    /// <returns><see langword="true" /> if the operands are equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(HostHeaderValue left, HostHeaderValue right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">First value.</param>
    /// <param name="right">Second value.</param>
    /// <returns><see langword="true" /> if the operands are unequal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(HostHeaderValue left, HostHeaderValue right);

    /// <summary>
    /// Parses a host header value.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="result">The parsed result.</param>
    /// <returns><see langword="true" /> if the value was parsed successfully, <see langword="false" /> otherwise.</returns>
    public static bool TryParse(string value, [NotNullWhen(true)] out HostHeaderValue result);

    /// <summary>
    /// Determines whether this host header value and a specified host header value are identical.
    /// </summary>
    /// <param name="other">The other host header value.</param>
    /// <returns><see langword="true" /> if the two values are identical; otherwise, <see langword="false" />.</returns>
    public bool Equals(HostHeaderValue other);

    /// <summary>
    /// Determines whether the specified object is equal to the current host header value.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> if the specified object is identical to the current host header value; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj);

    /// <summary>
    /// Gets a hash code for this object.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode();

    /// <summary>
    /// Gets a string representation of this object.
    /// </summary>
    /// <returns>A string representing this object.</returns>
    public override string ToString();
}
