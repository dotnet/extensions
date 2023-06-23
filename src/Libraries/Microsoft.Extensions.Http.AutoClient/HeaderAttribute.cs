// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Defines a header to be used in the API request.
/// </summary>
/// <remarks>
/// Marks a method parameter as a header to insert in the request.
/// </remarks>
/// <example>
/// <code>
/// [AutoClient("MyClient")]
/// interface IMyDependencyClient
/// {
///     [Get("/api/users")]
///     Task&lt;string&gt; GetUsersAsync([Header("X-UserName")] string userName, CancellationToken cancellationToken);
/// }
/// </code>
/// </example>
[Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
[AttributeUsage(AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class HeaderAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderAttribute"/> class.
    /// </summary>
    /// <param name="header">The name of the header.</param>
    public HeaderAttribute(string header)
    {
        Header = header;
    }

    /// <summary>
    /// Gets the name of the header.
    /// </summary>
    public string Header { get; }
}
