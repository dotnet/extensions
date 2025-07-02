// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides an optional base class for an <see cref="AIFunction"/> that passes through calls to another instance.
/// </summary>
public class DelegatingAIFunction : AIFunction
{
    private readonly AIFunction _innerFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingAIFunction"/> class as a wrapper around <paramref name="innerFunction"/>.
    /// </summary>
    /// <param name="innerFunction">The inner AI function to which all calls are delegated by default.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerFunction"/> is <see langword="null"/>.</exception>
    protected DelegatingAIFunction(AIFunction innerFunction)
    {
        _innerFunction = Throw.IfNull(innerFunction);
    }

    /// <inheritdoc />
    public override string Name => _innerFunction.Name;

    /// <inheritdoc />
    public override string Description => _innerFunction.Description;

    /// <inheritdoc />
    public override JsonElement JsonSchema => _innerFunction.JsonSchema;

    /// <inheritdoc />
    public override JsonElement? ReturnJsonSchema => _innerFunction.ReturnJsonSchema;

    /// <inheritdoc />
    public override JsonSerializerOptions JsonSerializerOptions => _innerFunction.JsonSerializerOptions;

    /// <inheritdoc />
    public override MethodInfo? UnderlyingMethod => _innerFunction.UnderlyingMethod;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => _innerFunction.AdditionalProperties;

    /// <inheritdoc />
    public override string ToString() => _innerFunction.ToString();

    /// <inheritdoc />
    protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken) =>
        _innerFunction.InvokeAsync(arguments, cancellationToken);
}
