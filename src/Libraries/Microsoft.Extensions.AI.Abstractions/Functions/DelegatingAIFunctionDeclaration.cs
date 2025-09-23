// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides an optional base class for an <see cref="AIFunctionDeclaration"/> that passes through calls to another instance.
/// </summary>
internal class DelegatingAIFunctionDeclaration : AIFunctionDeclaration // could be made public in the future if there's demand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingAIFunctionDeclaration"/> class as a wrapper around <paramref name="innerFunction"/>.
    /// </summary>
    /// <param name="innerFunction">The inner AI function to which all calls are delegated by default.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerFunction"/> is <see langword="null"/>.</exception>
    protected DelegatingAIFunctionDeclaration(AIFunctionDeclaration innerFunction)
    {
        InnerFunction = Throw.IfNull(innerFunction);
    }

    /// <summary>Gets the inner <see cref="AIFunctionDeclaration" />.</summary>
    protected AIFunctionDeclaration InnerFunction { get; }

    /// <inheritdoc />
    public override string Name => InnerFunction.Name;

    /// <inheritdoc />
    public override string Description => InnerFunction.Description;

    /// <inheritdoc />
    public override JsonElement JsonSchema => InnerFunction.JsonSchema;

    /// <inheritdoc />
    public override JsonElement? ReturnJsonSchema => InnerFunction.ReturnJsonSchema;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => InnerFunction.AdditionalProperties;

    /// <inheritdoc />
    public override string ToString() => InnerFunction.ToString();
}
