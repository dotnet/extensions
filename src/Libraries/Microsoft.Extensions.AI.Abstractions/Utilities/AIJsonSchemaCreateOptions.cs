// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;

#pragma warning disable S1067 // Expressions should not be too complex

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides options for configuring the behavior of <see cref="AIJsonUtilities"/> JSON schema creation functionality.
/// </summary>
public sealed record class AIJsonSchemaCreateOptions
{
    /// <summary>
    /// Gets the default options instance.
    /// </summary>
    public static AIJsonSchemaCreateOptions Default { get; } = new AIJsonSchemaCreateOptions();

    /// <summary>
    /// Gets a callback that is invoked for every schema that is generated within the type graph.
    /// </summary>
    public Func<AIJsonSchemaCreateContext, JsonNode, JsonNode>? TransformSchemaNode { get; init; }

    /// <summary>
    /// Gets a callback that is invoked for every parameter in the <see cref="MethodBase"/> provided to
    /// <see cref="AIJsonUtilities.CreateFunctionJsonSchema"/> in order to determine whether it should
    /// be included in the generated schema.
    /// </summary>
    /// <remarks>
    /// By default, when <see cref="IncludeParameter"/> is <see langword="null"/>, all parameters other
    /// than those of type <see cref="CancellationToken"/> are included in the generated schema.
    /// The delegate is not invoked for <see cref="CancellationToken"/> parameters.
    /// </remarks>
    public Func<ParameterInfo, bool>? IncludeParameter { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include the type keyword in inferred schemas for .NET enums.
    /// </summary>
    public bool IncludeTypeInEnumSchemas { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to generate schemas with the additionalProperties set to false for .NET objects.
    /// </summary>
    public bool DisallowAdditionalProperties { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to include the $schema keyword in inferred schemas.
    /// </summary>
    public bool IncludeSchemaKeyword { get; init; }

    /// <summary>
    /// Gets a value indicating whether to mark all properties as required in the schema.
    /// </summary>
    public bool RequireAllProperties { get; init; }
}
