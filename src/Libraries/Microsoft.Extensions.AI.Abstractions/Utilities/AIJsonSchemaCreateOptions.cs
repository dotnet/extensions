// Licensed to the .NET Foundation under one or more agreements.
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

    /// <summary>
    /// Gets a value indicating whether to mark all properties as required in the schema.
    /// </summary>
    public bool RequireAllProperties { get; init; }

    /// <summary>
    /// Gets a value indicating whether to filter keywords that are disallowed by certain AI vendors.
    /// </summary>
    /// <remarks>
    /// Filters a number of non-essential schema keywords that are not yet supported by some AI vendors.
    /// These include:
    /// <list type="bullet">
    /// <item>The "minLength", "maxLength", "pattern", and "format" keywords.</item>
    /// <item>The "minimum", "maximum", and "multipleOf" keywords.</item>
    /// <item>The "patternProperties", "unevaluatedProperties", "propertyNames", "minProperties", and "maxProperties" keywords.</item>
    /// <item>The "unevaluatedItems", "contains", "minContains", "maxContains", "minItems", "maxItems", and "uniqueItems" keywords.</item>
    /// </list>
    /// See also https://platform.openai.com/docs/guides/structured-outputs#some-type-specific-keywords-are-not-yet-supported.
    /// </remarks>
    public bool FilterDisallowedKeywords { get; init; }
}
