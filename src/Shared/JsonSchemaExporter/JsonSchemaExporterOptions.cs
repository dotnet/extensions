// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET9_0_OR_GREATER
using System;
using System.Text.Json.Nodes;

namespace System.Text.Json.Schema;

/// <summary>
/// Controls the behavior of the <see cref="JsonSchemaExporter"/> class.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal sealed class JsonSchemaExporterOptions
{
    /// <summary>
    /// Gets the default configuration object used by <see cref="JsonSchemaExporter"/>.
    /// </summary>
    public static JsonSchemaExporterOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether non-nullable schemas should be generated for null oblivious reference types.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false"/>. Due to restrictions in the run-time representation of nullable reference types
    /// most occurrences are null oblivious and are treated as nullable by the serializer. A notable exception to that rule
    /// are nullability annotations of field, property and constructor parameters which are represented in the contract metadata.
    /// </remarks>
    public bool TreatNullObliviousAsNonNullable { get; init; }

    /// <summary>
    /// Gets a callback that is invoked for every schema that is generated within the type graph.
    /// </summary>
    public Func<JsonSchemaExporterContext, JsonNode, JsonNode>? TransformSchemaNode { get; init; }
}
#endif
