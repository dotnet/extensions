// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a function that can be described to an AI service and invoked.</summary>
public abstract class AIFunction : AIFunctionDeclaration
{
    /// <summary>Initializes a new instance of the <see cref="AIFunction"/> class.</summary>
    protected AIFunction()
    {
    }

    /// <summary>
    /// Gets the underlying <see cref="MethodInfo"/> that this <see cref="AIFunction"/> might be wrapping.
    /// </summary>
    /// <remarks>
    /// Provides additional metadata on the function and its signature. Implementations not wrapping .NET methods may return <see langword="null"/>.
    /// </remarks>
    public virtual MethodInfo? UnderlyingMethod => null;

    /// <summary>Gets a <see cref="JsonSerializerOptions"/> that can be used to marshal function parameters.</summary>
    public virtual JsonSerializerOptions JsonSerializerOptions => AIJsonUtilities.DefaultOptions;

    /// <summary>Invokes the <see cref="AIFunction"/> and returns its result.</summary>
    /// <param name="arguments">The arguments to pass to the function's invocation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The result of the function's execution.</returns>
    public ValueTask<object?> InvokeAsync(
        AIFunctionArguments? arguments = null,
        CancellationToken cancellationToken = default) =>
        InvokeCoreAsync(arguments ?? [], cancellationToken);

    /// <summary>Invokes the <see cref="AIFunction"/> and returns its result.</summary>
    /// <param name="arguments">The arguments to pass to the function's invocation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The result of the function's execution.</returns>
    protected abstract ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken);

    /// <summary>Creates a <see cref="AIFunctionDeclaration"/> representation of this <see cref="AIFunction"/> that can't be invoked.</summary>
    /// <returns>The created instance.</returns>
    /// <remarks>
    /// <see cref="AIFunction"/> derives from <see cref="AIFunctionDeclaration"/>, layering on the ability to invoke the function in addition
    /// to describing it. <see cref="AsDeclarationOnly"/> creates a new object that describes the function but that can't be invoked.
    /// </remarks>
    public AIFunctionDeclaration AsDeclarationOnly() => new NonInvocableAIFunction(this);

    private sealed class NonInvocableAIFunction(AIFunction function) : DelegatingAIFunctionDeclaration(function);
}
