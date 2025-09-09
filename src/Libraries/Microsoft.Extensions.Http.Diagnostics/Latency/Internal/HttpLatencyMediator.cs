// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.Http.Latency.Internal;

/// <summary>
/// Mediator for HTTP latency operations that coordinates recording HTTP metrics in a latency context.
/// </summary>
internal class HttpLatencyMediator
{
    // Measure tokens
    private readonly MeasureToken _requestContentLength;
    private readonly MeasureToken _responseContentLength;

    // Tag tokens
    private readonly TagToken _httpMethod;
    private readonly TagToken _httpStatusCode;
    private readonly TagToken _requestHost;
    private readonly TagToken _requestPath;
    private readonly TagToken _responseContentType;
    private readonly TagToken _hasException;

    // Checkpoint tokens
    private readonly CheckpointToken _enricherInvoked;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpLatencyMediator"/> class.
    /// </summary>
    /// <param name="tokenIssuer">Token issuer for getting latency tokens.</param>
    public HttpLatencyMediator(ILatencyContextTokenIssuer tokenIssuer)
    {
        // Initialize checkpoint tokens
        _enricherInvoked = tokenIssuer.GetCheckpointToken(HttpCheckpoints.EnricherInvoked);

        // Initialize measure tokens
        _requestContentLength = tokenIssuer.GetMeasureToken("Http.Request.ContentLength");
        _responseContentLength = tokenIssuer.GetMeasureToken("Http.Response.ContentLength");

        // Initialize tag tokens
        _httpMethod = tokenIssuer.GetTagToken("Http.Method");
        _httpStatusCode = tokenIssuer.GetTagToken("Http.StatusCode");
        _requestHost = tokenIssuer.GetTagToken("Http.Request.Host");
        _requestPath = tokenIssuer.GetTagToken("Http.Request.Path");
        _responseContentType = tokenIssuer.GetTagToken("Http.Response.ContentType");
        _hasException = tokenIssuer.GetTagToken("Http.HasException");
    }

    /// <summary>
    /// Records HTTP request information in the latency context.
    /// </summary>
    /// <param name="context">The latency context to update.</param>
    /// <param name="request">The HTTP request message.</param>
    public virtual void RecordRequest(ILatencyContext context, HttpRequestMessage? request)
    {
        if (context == null || request == null)
        {
            return;
        }

        // Add checkpoint for request processing
        context.AddCheckpoint(_enricherInvoked);

        // Collect request-related data
        context.SetTag(_httpMethod, request.Method.Method);

        if (request.RequestUri != null)
        {
            context.SetTag(_requestHost, request.RequestUri.Host);
            context.SetTag(_requestPath, request.RequestUri.AbsolutePath);
        }

        // Collect request content length if available
        if (request.Content?.Headers.ContentLength.HasValue == true)
        {
            context.RecordMeasure(_requestContentLength, request.Content.Headers.ContentLength.Value);
        }
    }

    /// <summary>
    /// Records HTTP response information in the latency context.
    /// </summary>
    /// <param name="context">The latency context to update.</param>
    /// <param name="response">The HTTP response message.</param>
    public virtual void RecordResponse(ILatencyContext context, HttpResponseMessage response)
    {
        if (context == null || response == null)
        {
            return;
        }

        // Add response-related data with culture-invariant string conversion
        context.SetTag(_httpStatusCode, ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture));

        // Collect response content type if available
        if (response.Content?.Headers.ContentType != null)
        {
            context.SetTag(_responseContentType, response.Content.Headers.ContentType.MediaType);
        }

        // Collect response content length if available
        if (response.Content?.Headers.ContentLength.HasValue == true)
        {
            context.RecordMeasure(_responseContentLength, response.Content.Headers.ContentLength.Value);
        }
    }

    /// <summary>
    /// Records exception information in the latency context.
    /// </summary>
    /// <param name="context">The latency context to update.</param>
    /// <param name="exception">The exception that occurred, if any.</param>
    public virtual void RecordException(ILatencyContext context, Exception? exception)
    {
        if (context == null)
        {
            return;
        }

        context.SetTag(_hasException, exception != null ? "true" : "false");
    }

    /// <summary>
    /// Appends checkpoint data to the provided string builder.
    /// </summary>
    /// <param name="context">The latency context containing checkpoint data.</param>
    /// <param name="stringBuilder">The string builder to append data to.</param>
    public virtual void AppendCheckpoints(ILatencyContext context, StringBuilder stringBuilder)
    {
        if (context == null || stringBuilder == null)
        {
            return;
        }

        var latencyData = context.LatencyData;
        for (int i = 0; i < latencyData.Checkpoints.Length; i++)
        {
            _ = stringBuilder.Append(latencyData.Checkpoints[i].Name);
            _ = stringBuilder.Append('/');
        }

        _ = stringBuilder.Append(',');
        for (int i = 0; i < latencyData.Checkpoints.Length; i++)
        {
            var ms = (double)latencyData.Checkpoints[i].Elapsed / latencyData.Checkpoints[i].Frequency * 1000;
            _ = stringBuilder.Append(ms.ToString(CultureInfo.InvariantCulture));
            _ = stringBuilder.Append('/');
        }
    }
}

