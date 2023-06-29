// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Overrides the request name of a REST API method.
/// </summary>
/// <remarks>
/// When this attribute is used on a REST API method, it overrides the request name of that method.
/// If this attribute is not provided, the request name is obtained from the method name. If the method name ends in 'Async', the request name will exclude that.
/// For example, if the method is called <c>GetUsersAsync</c> the request name, by default, will be <c>GetUsers</c>.
/// </remarks>
/// <example>
/// <code>
/// [AutoClient("MyClient")]
/// interface IMyDependencyClient
/// {
///     [Get("/api/users")]
///     [RequestName("ObtainUsers")]
///     Task&lt;string&gt; GetUsersAsync(CancellationToken cancellationToken = default);
/// }
/// </code>
/// </example>
[Experimental(diagnosticId: "TBD", UrlFormat = WarningDefinitions.SharedUrlFormat)]
[AttributeUsage(AttributeTargets.Method)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class RequestNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestNameAttribute"/> class.
    /// </summary>
    /// <param name="value">The name to use for this request within telemetry.</param>
    public RequestNameAttribute(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the value to be used as request name in telemetry.
    /// </summary>
    public string Value { get; }
}
