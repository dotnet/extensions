// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Runtime.CompilerServices;

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
    public int? StatusCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.AutoClient.AutoClientException" /> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="path">The path of the request.</param>
    /// <param name="error">The HTTP error details.</param>
    public AutoClientException(string? message, string path, AutoClientHttpError? error = null);
}
