// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Telemetry.Internal;

#pragma warning disable CA1815 // Override equals and operator equals on value types
/// <summary>
/// Struct to hold metadata about a route parameter.
/// </summary>
internal readonly struct HttpRouteParameter
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRouteParameter"/> struct.
    /// </summary>
    /// <param name="name">parameter name.</param>
    /// <param name="value">parameter value.</param>
    /// <param name="isRedacted">conveys if the parameter value is redacted.</param>
    public HttpRouteParameter(string name, string value, bool isRedacted)
    {
        Name = name;
        Value = value;
        IsRedacted = isRedacted;
    }

    /// <summary>
    /// Gets parameter name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets parameter value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets a value indicating whether the parameter value is redacted.
    /// </summary>
    public bool IsRedacted { get; }
}
