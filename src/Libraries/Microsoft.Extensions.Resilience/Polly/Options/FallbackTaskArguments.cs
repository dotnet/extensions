// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Structure with the arguments of the on bulkhead task.
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types (Such usage is not expected in this scenario)
public readonly struct FallbackTaskArguments : IPolicyEventArguments
#pragma warning restore CA1815
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackTaskArguments" /> structure.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="context">The policy context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public FallbackTaskArguments(
        Exception exception,
        Context context,
        CancellationToken cancellationToken)
    {
        Context = Throw.IfNull(context);
        Exception = Throw.IfNull(exception);
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the result of the action executed by the retry policy.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the Polly <see cref="global::Polly.Context" /> associated with the policy execution.
    /// </summary>
    public Context Context { get; }

    /// <summary>
    /// Gets the cancellation token associated with the policy execution.
    /// </summary>
    public CancellationToken CancellationToken { get; }
}
