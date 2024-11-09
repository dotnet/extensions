﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides options for configuring the behavior of <see cref="AIJsonUtilities"/> JSON schema creation functionality.
/// </summary>
public sealed class AIJsonSchemaCreateOptions
{
    /// <summary>
    /// Gets the default options instance.
    /// </summary>
    public static AIJsonSchemaCreateOptions Default { get; } = new AIJsonSchemaCreateOptions();

    /// <summary>
    /// Gets a value indicating whether to include the type keyword in inferred schemas for .NET enums.
    /// </summary>
    public bool IncludeTypeInEnumSchemas { get; init; }

    /// <summary>
    /// Gets a value indicating whether to generate schemas with the additionalProperties set to false for .NET objects.
    /// </summary>
    public bool DisallowAdditionalProperties { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include the $schema keyword in inferred schemas.
    /// </summary>
    public bool IncludeSchemaKeyword { get; init; }
}
