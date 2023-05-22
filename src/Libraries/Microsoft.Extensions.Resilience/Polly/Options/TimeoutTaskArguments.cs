// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Structure with the arguments of the on timeout task.
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types (Such usage is not expected in this scenario)
public readonly struct TimeoutTaskArguments : IPolicyEventArguments
#pragma warning restore CA1815
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutTaskArguments" /> structure.
    /// </summary>
    /// <param name="context">The policy context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public TimeoutTaskArguments(
        Context context,
        CancellationToken cancellationToken)
    {
        Context = Throw.IfNull(context);
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the Polly <see cref="global::Polly.Context" /> associated with the policy execution.
    /// </summary>
    public Context Context { get; }

    /// <summary>
    /// Gets the cancellation token associated with the policy execution.
    /// </summary>
    public CancellationToken CancellationToken { get; }
}
