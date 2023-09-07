// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Holds details about an HTTP error.
/// </summary>
/// <remarks>
/// When a REST API client fails, it will throw a <see cref="T:Microsoft.Extensions.Http.AutoClient.AutoClientException" />.
/// This exception contains a <see cref="T:Microsoft.Extensions.Http.AutoClient.AutoClientHttpError" /> instance that holds details like content, headers and status code.
/// </remarks>
public class AutoClientHttpError
{
    /// <summary>
    /// Gets the HTTP status code returned in the response.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the HTTP response headers.
    /// </summary>
    public IReadOnlyDictionary<string, StringValues> ResponseHeaders { get; }

    /// <summary>
    /// Gets the raw string content returned in the response.
    /// </summary>
    public string RawContent { get; }

    /// <summary>
    /// Gets the HTTP error reason.
    /// </summary>
    public string? ReasonPhrase { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.AutoClient.AutoClientHttpError" /> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="responseHeaders">The response headers.</param>
    /// <param name="rawContent">The raw string content of the response.</param>
    /// <param name="reasonPhrase">The HTTP error reason.</param>
    public AutoClientHttpError(int statusCode, IReadOnlyDictionary<string, StringValues> responseHeaders, string rawContent, string? reasonPhrase);

    /// <summary>
    /// Creates an instance of <see cref="T:Microsoft.Extensions.Http.AutoClient.AutoClientHttpError" /> based on an <see cref="T:System.Net.Http.HttpResponseMessage" />.
    /// </summary>
    /// <param name="response">The response to be used.</param>
    /// <param name="cancellationToken">Cancellation token used on asynchronous calls.</param>
    /// <returns>An instance of <see cref="T:Microsoft.Extensions.Http.AutoClient.AutoClientHttpError" />.</returns>
    public static Task<AutoClientHttpError> CreateAsync(HttpResponseMessage response, CancellationToken cancellationToken);
}
