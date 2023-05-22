// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Defines a static header to be sent on every API request.
/// </summary>
/// <remarks>
/// Injects a static header to be sent with every request. When this attribute is applied
/// to an interface, then it impacts every method described by the interface. Otherwise, it only
/// applies to the method where it is applied.
/// </remarks>
/// <example>
/// <code>
/// [AutoClient("MyClient")]
/// [StaticHeader("X-MyHeader", "MyHeaderValue")]
/// interface IMyDependencyClient
/// {
///     [Get("/api/users")]
///     [StaticHeader("X-GetUsersHeader", "Value")]
///     public Task&lt;Users&gt; GetUsers(CancellationToken cancellationToken = default);
/// }
/// </code>
/// </example>
[Experimental]
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class StaticHeaderAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StaticHeaderAttribute"/> class.
    /// </summary>
    /// <param name="header">The name of the header.</param>
    /// <param name="value">The value of the header.</param>
    public StaticHeaderAttribute(string header, string value)
    {
        Header = header;
        Value = value;
    }

    /// <summary>
    /// Gets the name of the header.
    /// </summary>
    public string Header { get; }

    /// <summary>
    /// Gets the value of the header.
    /// </summary>
    public string Value { get; }
}
