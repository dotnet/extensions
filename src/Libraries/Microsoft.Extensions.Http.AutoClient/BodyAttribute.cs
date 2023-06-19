// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Defines the body for the API request.
/// </summary>
/// <remarks>
/// Marks a method parameter as the body for the request.
/// This attribute cannot be used with a GET or HEAD request.
/// </remarks>
/// <example>
/// <code>
/// [AutoClient("MyClient")]
/// interface IMyDependencyClient
/// {
///     [Post("/api/users")]
///     Task&lt;User&gt; PostUserAsync([Body] User user, CancellationToken cancellationToken);
/// }
/// </code>
/// </example>
[Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
[AttributeUsage(AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class BodyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BodyAttribute"/> class.
    /// </summary>
    /// <remarks>
    /// This defaults to a body content type of <see cref="BodyContentType.ApplicationJson"/>.
    /// </remarks>
    public BodyAttribute()
        : this(BodyContentType.ApplicationJson)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BodyAttribute"/> class.
    /// </summary>
    /// <param name="contentType">The content type to be used on the request content.</param>
    public BodyAttribute(BodyContentType contentType)
    {
        ContentType = contentType;
    }

    /// <summary>
    /// Gets the body content type.
    /// </summary>
    public BodyContentType ContentType { get; }
}
