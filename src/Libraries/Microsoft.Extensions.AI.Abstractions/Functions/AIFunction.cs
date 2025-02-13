// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Collections;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a function that can be described to an AI service and invoked.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public abstract class AIFunction : AITool
{
    /// <summary>Gets the name of the function.</summary>
    public abstract string Name { get; }

    /// <summary>Gets a description of the function, suitable for use in describing the purpose to a model.</summary>
    public abstract string Description { get; }

    /// <summary>Gets a JSON Schema describing the function and its input parameters.</summary>
    /// <remarks>
    /// <para>
    /// When specified, declares a self-contained JSON schema document that describes the function and its input parameters.
    /// A simple example of a JSON schema for a function that adds two numbers together is shown below:
    /// </para>
    /// <code>
    /// {
    ///   "title" : "addNumbers",
    ///   "description": "A simple function that adds two numbers together.",
    ///   "type": "object",
    ///   "properties": {
    ///     "a" : { "type": "number" },
    ///     "b" : { "type": "number", "default": 1 }
    ///   }, 
    ///   "required" : ["a"]
    /// }
    /// </code>
    /// <para>
    /// The metadata present in the schema document plays an important role in guiding AI function invocation.
    /// </para>
    /// <para>
    /// When no schema is specified, consuming chat clients should assume the "{}" or "true" schema, indicating that any JSON input is admissible.
    /// </para>
    /// </remarks>
    public virtual JsonElement JsonSchema => AIJsonUtilities.DefaultJsonSchema;

    /// <summary>
    /// Gets the underlying <see cref="MethodInfo"/> that this <see cref="AIFunction"/> might be wrapping.
    /// </summary>
    /// <remarks>
    /// Provides additional metadata on the function and its signature. Implementations not wrapping .NET methods may return <see langword="null"/>.
    /// </remarks>
    public virtual MethodInfo? UnderlyingMethod => null;

    /// <summary>Gets any additional properties associated with the function.</summary>
    public virtual IReadOnlyDictionary<string, object?> AdditionalProperties => EmptyReadOnlyDictionary<string, object?>.Instance;

    /// <summary>Gets a <see cref="JsonSerializerOptions"/> that can be used to marshal function parameters.</summary>
    public virtual JsonSerializerOptions? JsonSerializerOptions => AIJsonUtilities.DefaultOptions;

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
    public override string ToString() => Name;

    /// <summary>Invokes the <see cref="AIFunction"/> and returns its result.</summary>
    /// <param name="arguments">The arguments to pass to the function's invocation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The result of the function's execution.</returns>
    protected abstract Task<object?> InvokeCoreAsync(
        IEnumerable<KeyValuePair<string, object?>> arguments,
        CancellationToken cancellationToken);

    /// <summary>Gets the string to display in the debugger for this instance.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} ({Description})";
}
