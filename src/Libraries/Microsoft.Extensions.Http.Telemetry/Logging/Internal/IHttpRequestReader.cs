// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

/// <summary>
/// Methods to read <see cref="HttpResponseMessage"/> or <see cref="HttpRequestMessage"/>.
/// </summary>
internal interface IHttpRequestReader
{
    /// <summary>
    /// Reads <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="record">A log record object to fill in.</param>
    /// <param name="response">HTTP response.</param>
    /// <param name="responseHeadersBuffer">A buffer to read response headers to.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <returns>A task representing an async operation.</returns>
    Task ReadResponseAsync(LogRecord record, HttpResponseMessage response,
        List<KeyValuePair<string, string>>? responseHeadersBuffer,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads <see cref="HttpRequestMessage"/>.
    /// </summary>
    /// <param name="record">A log record object to fill in.</param>
    /// <param name="request">HTTP request.</param>
    /// <param name="requestHeadersBuffer">A buffer to read request headers to.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <returns>A task representing an async operation.</returns>
    Task ReadRequestAsync(LogRecord record, HttpRequestMessage request,
        List<KeyValuePair<string, string>>? requestHeadersBuffer,
        CancellationToken cancellationToken);
}
