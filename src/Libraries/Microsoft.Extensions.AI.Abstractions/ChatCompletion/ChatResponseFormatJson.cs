// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a response format for structured JSON data.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class ChatResponseFormatJson : ChatResponseFormat
{
    /// <summary>Initializes a new instance of the <see cref="ChatResponseFormatJson"/> class with the specified schema.</summary>
    /// <param name="schema">The schema to associate with the JSON response.</param>
    /// <param name="schemaName">A name for the schema.</param>
    /// <param name="schemaDescription">A description of the schema.</param>
    [JsonConstructor]
    public ChatResponseFormatJson(
        [StringSyntax(StringSyntaxAttribute.Json)] string? schema, string? schemaName = null, string? schemaDescription = null)
    {
        if (schema is null && (schemaName is not null || schemaDescription is not null))
        {
            Throw.ArgumentException(
                schemaName is not null ? nameof(schemaName) : nameof(schemaDescription),
                "Schema name and description can only be specified if a schema is provided.");
        }

        Schema = schema;
        SchemaName = schemaName;
        SchemaDescription = schemaDescription;
    }

    /// <summary>Gets the JSON schema associated with the response, or null if there is none.</summary>
    public string? Schema { get; }

    /// <summary>Gets a name for the schema.</summary>
    public string? SchemaName { get; }

    /// <summary>Gets a description of the schema.</summary>
    public string? SchemaDescription { get; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is ChatResponseFormatJson other &&
        Schema == other.Schema &&
        SchemaName == other.SchemaName &&
        SchemaDescription == other.SchemaDescription;

    /// <inheritdoc/>
    public override int GetHashCode() =>
        Schema?.GetHashCode(StringComparison.Ordinal) ??
        typeof(ChatResponseFormatJson).GetHashCode();

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => Schema ?? "JSON";
}
