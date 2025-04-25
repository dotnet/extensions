// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the response format that is desired by the caller.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ChatResponseFormatText), typeDiscriminator: "text")]
[JsonDerivedType(typeof(ChatResponseFormatJson), typeDiscriminator: "json")]
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
public class ChatResponseFormat
#pragma warning restore CA1052
{
    /// <summary>Initializes a new instance of the <see cref="ChatResponseFormat"/> class.</summary>
    /// <remarks>Prevents external instantiation. Close the inheritance hierarchy for now until we have good reason to open it.</remarks>
    private protected ChatResponseFormat()
    {
    }

    /// <summary>Gets a singleton instance representing unstructured textual data.</summary>
    public static ChatResponseFormatText Text { get; } = new();

    /// <summary>Gets a singleton instance representing structured JSON data but without any particular schema.</summary>
    public static ChatResponseFormatJson Json { get; } = new(schema: null);

    /// <summary>Creates a <see cref="ChatResponseFormatJson"/> representing structured JSON data with the specified schema.</summary>
    /// <param name="schema">The JSON schema.</param>
    /// <param name="schemaName">An optional name of the schema. For example, if the schema represents a particular class, this could be the name of the class.</param>
    /// <param name="schemaDescription">An optional description of the schema.</param>
    /// <returns>The <see cref="ChatResponseFormatJson"/> instance.</returns>
    public static ChatResponseFormatJson ForJsonSchema(
        JsonElement schema, string? schemaName = null, string? schemaDescription = null) =>
        new(schema,
            schemaName,
            schemaDescription);
}
