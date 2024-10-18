// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Collections;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a function that can be described to an AI service and invoked.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public abstract class AIFunction : AITool
{
    /// <summary>Gets metadata describing the function.</summary>
    public abstract AIFunctionMetadata Metadata { get; }

    /// <summary>Invokes the <see cref="AIFunction"/> and returns its result.</summary>
    /// <param name="arguments">The arguments to pass to the function's invocation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The result of the function's execution.</returns>
    public Task<object?> InvokeAsync(
        IEnumerable<KeyValuePair<string, object?>>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        arguments ??= EmptyReadOnlyDictionary<string, object?>.Instance;

        return InvokeCoreAsync(arguments, cancellationToken);
    }

    /// <inheritdoc/>
    public override string ToString() => Metadata.Name;

    /// <summary>Invokes the <see cref="AIFunction"/> and returns its result.</summary>
    /// <param name="arguments">The arguments to pass to the function's invocation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The result of the function's execution.</returns>
    protected abstract Task<object?> InvokeCoreAsync(
        IEnumerable<KeyValuePair<string, object?>> arguments,
        CancellationToken cancellationToken);

    /// <summary>Gets the string to display in the debugger for this instance.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        string.IsNullOrWhiteSpace(Metadata.Description) ?
            Metadata.Name :
            $"{Metadata.Name} ({Metadata.Description})";
}
