// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Exception used whenever REST API requests are not successful.
/// </summary>
/// <remarks>
/// This exception is thrown whenever a REST API call returns a non-successful status code. It contains the status code
/// and the HTTP content returned by the dependency, so that the user can handle exceptions accordingly.
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     await _myClient.SendRequest();
/// }
/// catch (AutoClientException ex) when (ex.StatusCode == 403)
/// {
///     // Handle forbidden scenario
/// }
/// </code>
/// </example>
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Not applicable to this exception")]
public class AutoClientException : Exception
{
    /// <summary>
    /// Gets the HTTP response.
    /// </summary>
    public AutoClientHttpError? HttpError { get; }

    /// <summary>
    /// Gets the initial path of the HTTP request.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int? StatusCode => HttpError?.StatusCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoClientException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="path">The path of the request.</param>
    /// <param name="error">The HTTP error details.</param>
    public AutoClientException(string? message, string path, AutoClientHttpError? error = null)
        : base(message)
    {
        Path = Throw.IfNull(path);
        HttpError = error;
    }
}
