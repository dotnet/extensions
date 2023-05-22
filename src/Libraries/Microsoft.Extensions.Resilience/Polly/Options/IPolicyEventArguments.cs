// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Polly;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Lexical interface for all non-generic policy arguments.
/// Do not use outside Argument struct header to avoid overhead.
/// </summary>
internal interface IPolicyEventArguments
{
    /// <summary>
    /// Gets policy argument cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the Polly <see cref="Polly.Context" /> associated with the event.
    /// </summary>
    public Context Context { get; }
}
