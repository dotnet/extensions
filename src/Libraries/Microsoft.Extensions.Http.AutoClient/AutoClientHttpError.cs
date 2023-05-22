// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Holds details about an HTTP error.
/// </summary>
/// <remarks>
/// When a REST API client fails, it will throw a <see cref="AutoClientException"/>.
/// This exception contains a <see cref="AutoClientHttpError"/> instance that holds details like content, headers and status code.
/// </remarks>
[Experimental]
public class AutoClientHttpError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoClientHttpError"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="responseHeaders">The response headers.</param>
    /// <param name="rawContent">The raw string content of the response.</param>
    /// <param name="reasonPhrase">The HTTP error reason.</param>
    public AutoClientHttpError(int statusCode, IReadOnlyDictionary<string, StringValues> responseHeaders, string rawContent, string? reasonPhrase)
    {
        StatusCode = Throw.IfNull(statusCode);
        ResponseHeaders = Throw.IfNull(responseHeaders);
        RawContent = Throw.IfNull(rawContent);
        ReasonPhrase = reasonPhrase;
    }

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
    /// Creates an instance of <see cref="AutoClientHttpError"/> based on an <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="response">The response to be used.</param>
    /// <param name="cancellationToken">Cancellation token used on asynchronous calls.</param>
    /// <returns>An instance of <see cref="AutoClientHttpError"/>.</returns>
    public static async Task<AutoClientHttpError> CreateAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        Throw.IfNull(response);

#if NET5_0_OR_GREATER
        var rawContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
        cancellationToken.ThrowIfCancellationRequested();
        var rawContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        var responseHeaders = response.Headers.ToDictionary(p => p.Key, p => new StringValues(p.Value.ToArray()));

        foreach (var header in response.Content.Headers)
        {
            responseHeaders[header.Key] = new StringValues(header.Value.ToArray());
        }

        return new AutoClientHttpError((int)response.StatusCode, responseHeaders, rawContent, response.ReasonPhrase);
    }
}
