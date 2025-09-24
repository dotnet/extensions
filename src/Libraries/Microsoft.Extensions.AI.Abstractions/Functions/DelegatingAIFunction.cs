// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides an optional base class for an <see cref="AIFunction"/> that passes through calls to another instance.
/// </summary>
public class DelegatingAIFunction : AIFunction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingAIFunction"/> class as a wrapper around <paramref name="innerFunction"/>.
    /// </summary>
    /// <param name="innerFunction">The inner AI function to which all calls are delegated by default.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerFunction"/> is <see langword="null"/>.</exception>
    protected DelegatingAIFunction(AIFunction innerFunction)
    {
        InnerFunction = Throw.IfNull(innerFunction);
    }

    /// <summary>Gets the inner <see cref="AIFunction" />.</summary>
    protected AIFunction InnerFunction { get; }

    /// <inheritdoc />
    public override string Name => InnerFunction.Name;

    /// <inheritdoc />
    public override string Description => InnerFunction.Description;

    /// <inheritdoc />
    public override JsonElement JsonSchema => InnerFunction.JsonSchema;

    /// <inheritdoc />
    public override JsonElement? ReturnJsonSchema => InnerFunction.ReturnJsonSchema;

    /// <inheritdoc />
    public override JsonSerializerOptions JsonSerializerOptions => InnerFunction.JsonSerializerOptions;

    /// <inheritdoc />
    public override MethodInfo? UnderlyingMethod => InnerFunction.UnderlyingMethod;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => InnerFunction.AdditionalProperties;

    /// <inheritdoc />
    public override string ToString() => InnerFunction.ToString();

    /// <inheritdoc />
    protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken) =>
        InnerFunction.InvokeAsync(arguments, cancellationToken);

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
            InnerFunction.GetService(serviceType, serviceKey);
    }
}
