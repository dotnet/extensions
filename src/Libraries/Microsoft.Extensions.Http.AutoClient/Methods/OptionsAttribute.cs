﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Defines an API OPTIONS method.
/// </summary>
/// <remarks>
/// Marks a method within an interface annotated with <see cref="AutoClientAttribute"/> as an API OPTIONS method.
///
/// The return type of an API method must be a <c>Task&lt;T&gt;</c>.
/// If T is a <see cref="string"/> and the dependency returns "text/plain" content type, the result will be the raw content of the response. Otherwise, it will be deserialized from JSON.
/// If T is of type <see cref="HttpResponseMessage"/>, the result will be the actual response message without further processing.
///
/// If you provide an extra parameter to the method, you should use it between curly braces in the URL to make it an URL parameter. For example: <c>/api/users/{userId}</c>.
/// </remarks>
/// <example>
/// <code>
/// [AutoClient("MyClient")]
/// interface IMyDependencyClient
/// {
///     [Options("/api/users/{userId}")]
///     Task&lt;UserOptions&gt; UserOptionsAsync(string userId, CancellationToken cancellationToken);
/// }
/// </code>
/// </example>
[Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
[AttributeUsage(AttributeTargets.Method)]
public sealed class OptionsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsAttribute"/> class.
    /// </summary>
    /// <param name="path">The path of the request.</param>
    public OptionsAttribute(string path)
    {
        Path = path;
    }

    /// <summary>
    /// Gets the path of the request.
    /// </summary>
    public string Path { get; }
}
