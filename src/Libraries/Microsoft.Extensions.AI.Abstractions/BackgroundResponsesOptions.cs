// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>Options for configuring background responses.</summary>
[Experimental("MEAI001")]
public sealed class BackgroundResponsesOptions
{
    /// <summary>Gets or sets a value indicating whether the background responses are allowed.</summary>
    /// <remarks>
    /// <para>
    /// Background responses allow running long-running operations or tasks asynchronously in the background that can be resumed by streaming APIs
    /// and polled for completion by non-streaming APIs.
    /// </para>
    /// <para>
    /// When this property is set to true, non-streaming APIs start a background operation and return an initial
    /// response with a continuation token. Subsequent calls to the same API should be made in a polling manner with
    /// the continuation token to get the final result of the operation.
    /// </para>
    /// <para>
    /// When this property is set to true, streaming APIs also start a background operation and begin streaming
    /// response updates until the operation is completed. If the streaming connection is interrupted, the
    /// continuation token obtained from the last update should be supplied to a subsequent call to the same streaming API
    /// to resume the stream from the point of interruption and continue receiving updates until the operation is completed.
    /// </para>
    /// <para>
    /// This property only takes effect if the API it's used with supports background responses.
    /// If the API does not support background responses, this property will be ignored.
    /// </para>
    /// </remarks>
    public bool? Allow { get; set; }
}
