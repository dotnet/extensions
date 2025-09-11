// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.Http.Latency.Internal;

/// <summary>
/// Interface for mediating HTTP latency operations that coordinates recording HTTP metrics in a latency context.
/// </summary>
internal interface IHttpLatencyMediator
{
    /// <summary>
    /// Records the start of an HTTP request in the latency context.
    /// </summary>
    /// <param name="context">The latency context to update.</param>
    /// <param name="request">The HTTP request message.</param>
    void RecordStart(ILatencyContext context, HttpRequestMessage request);

    /// <summary>
    /// Records the end of an HTTP request/response cycle in the latency context.
    /// </summary>
    /// <param name="context">The latency context to update.</param>
    /// <param name="request">The HTTP request message (optional if already recorded in RecordStart).</param>
    /// <param name="response">The HTTP response message.</param>
    void RecordEnd(ILatencyContext context, HttpRequestMessage? request = null, HttpResponseMessage? response = null);

    /// <summary>
    /// Appends checkpoint data to the provided string builder.
    /// </summary>
    /// <param name="context">The latency context containing checkpoint data.</param>
    /// <param name="stringBuilder">The string builder to append data to.</param>
    void AppendCheckpoints(ILatencyContext context, StringBuilder stringBuilder);
}