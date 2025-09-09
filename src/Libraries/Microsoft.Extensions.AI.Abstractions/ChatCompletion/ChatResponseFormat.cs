// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.Extensions.AI;

#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
#pragma warning disable S2333 // gratuitous partial

/// <summary>Represents the response format that is desired by the caller.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ChatResponseFormatText), typeDiscriminator: "text")]
[JsonDerivedType(typeof(ChatResponseFormatJson), typeDiscriminator: "json")]
public partial class ChatResponseFormat
{
    private static readonly AIJsonSchemaCreateOptions _inferenceOptions = new()
    {
        IncludeSchemaKeyword = true,
        TransformOptions = new AIJsonSchemaTransformOptions
        {
            DisallowAdditionalProperties = true,
            RequireAllProperties = true,
            MoveDefaultKeywordToDescription = true,
        },
    };

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
        new(schema, schemaName, schemaDescription);

    /// <summary>Creates a <see cref="ChatResponseFormatJson"/> representing structured JSON data with the specified schema.</summary>
    /// <typeparam name="T">The type for which a schema should be exported and used as the response schema.</typeparam>
    /// <param name="serializerOptions">The JSON serialization options to use.</param>
    /// <param name="schemaName">An optional name of the schema. By default, this will be inferred from <typeparamref name="T"/>.</param>
    /// <param name="schemaDescription">An optional description of the schema. By default, this will be inferred from <typeparamref name="T"/>.</param>
    /// <returns>The <see cref="ChatResponseFormatJson"/> instance.</returns>
    /// <remarks>
    /// Many AI services that support structured output require that the JSON schema have a top-level 'type=object'.
    /// If <typeparamref name="T"/> is a primitive type like <see cref="string"/>, <see cref="int"/>, or <see cref="bool"/>,
    /// or if it's a type that serializes as a JSON array, attempting to use the resulting schema with such services may fail.
    /// In such cases, consider instead using a <typeparamref name="T"/> that wraps the actual type in a class or struct so that
    /// it serializes as a JSON object with the original type as a property of that object.
    /// </remarks>
    public static ChatResponseFormatJson ForJsonSchema<T>(
        JsonSerializerOptions? serializerOptions = null, string? schemaName = null, string? schemaDescription = null)
    {
        var schema = AIJsonUtilities.CreateJsonSchema(
            type: typeof(T),
            serializerOptions: serializerOptions ?? AIJsonUtilities.DefaultOptions,
            inferenceOptions: _inferenceOptions);

        return ForJsonSchema(
            schema,
            schemaName ?? InvalidNameCharsRegex().Replace(typeof(T).Name, "_"),
            schemaDescription ?? typeof(T).GetCustomAttribute<DescriptionAttribute>()?.Description);
    }

    /// <summary>Regex that flags any character other than ASCII digits, ASCII letters, or underscore.</summary>
#if NET
    [GeneratedRegex("[^0-9A-Za-z_]")]
    private static partial Regex InvalidNameCharsRegex();
#else
    private static Regex InvalidNameCharsRegex() => _invalidNameCharsRegex;
    private static readonly Regex _invalidNameCharsRegex = new("[^0-9A-Za-z_]", RegexOptions.Compiled);
#endif
}
