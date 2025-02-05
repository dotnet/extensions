// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using Microsoft.Shared.Collections;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides read-only metadata for an <see cref="AIFunction"/>.
/// </summary>
public sealed class AIFunctionMetadata
{
    /// <summary>The name of the function.</summary>
    private readonly string _name = string.Empty;

    /// <summary>The description of the function.</summary>
    private readonly string _description = string.Empty;

    /// <summary>The JSON schema describing the function and its input parameters.</summary>
    private readonly JsonElement _schema = AIJsonUtilities.DefaultJsonSchema;

    /// <summary>The function's parameters.</summary>
    private readonly IReadOnlyList<AIFunctionParameterMetadata> _parameters = [];

    /// <summary>The function's return parameter.</summary>
    private readonly AIFunctionReturnParameterMetadata _returnParameter = AIFunctionReturnParameterMetadata.Empty;

    /// <summary>Optional additional properties in addition to the named properties already available on this class.</summary>
    private readonly IReadOnlyDictionary<string, object?> _additionalProperties = EmptyReadOnlyDictionary<string, object?>.Instance;

    /// <summary><see cref="_parameters"/> indexed by name, lazily initialized.</summary>
    private Dictionary<string, AIFunctionParameterMetadata>? _parametersByName;

    /// <summary>Initializes a new instance of the <see cref="AIFunctionMetadata"/> class for a function with the specified name.</summary>
    /// <param name="name">The name of the function.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="name"/> was null.</exception>
    public AIFunctionMetadata(string name)
    {
        _name = Throw.IfNullOrWhitespace(name);
    }

    /// <summary>Initializes a new instance of the <see cref="AIFunctionMetadata"/> class as a copy of another <see cref="AIFunctionMetadata"/>.</summary>
    /// <exception cref="ArgumentNullException">The <paramref name="metadata"/> was null.</exception>
    /// <remarks>
    /// This creates a shallow clone of <paramref name="metadata"/>. The new instance's <see cref="Parameters"/> and
    /// <see cref="ReturnParameter"/> properties will return the same objects as in the original instance.
    /// </remarks>
    public AIFunctionMetadata(AIFunctionMetadata metadata)
    {
        Name = Throw.IfNull(metadata).Name;
        Description = metadata.Description;
        Parameters = metadata.Parameters;
        ReturnParameter = metadata.ReturnParameter;
        AdditionalProperties = metadata.AdditionalProperties;
        Schema = metadata.Schema;
    }

    /// <summary>Gets the name of the function.</summary>
    public string Name
    {
        get => _name;
        init => _name = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>Gets a description of the function, suitable for use in describing the purpose to a model.</summary>
    [AllowNull]
    public string Description
    {
        get => _description;
        init => _description = value ?? string.Empty;
    }

    /// <summary>Gets the metadata for the parameters to the function.</summary>
    /// <remarks>If the function has no parameters, the returned list is empty.</remarks>
    public IReadOnlyList<AIFunctionParameterMetadata> Parameters
    {
        get => _parameters;
        init => _parameters = Throw.IfNull(value);
    }

    /// <summary>Gets the <see cref="AIFunctionParameterMetadata"/> for a parameter by its name.</summary>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>The corresponding <see cref="AIFunctionParameterMetadata"/>, if found; otherwise, null.</returns>
    public AIFunctionParameterMetadata? GetParameter(string name)
    {
        Dictionary<string, AIFunctionParameterMetadata>? parametersByName = _parametersByName ??= _parameters.ToDictionary(p => p.Name);

        return parametersByName.TryGetValue(name, out AIFunctionParameterMetadata? parameter) ?
            parameter :
            null;
    }

    /// <summary>Gets parameter metadata for the return parameter.</summary>
    /// <remarks>If the function has no return parameter, the value is a default instance of an <see cref="AIFunctionReturnParameterMetadata"/>.</remarks>
    public AIFunctionReturnParameterMetadata ReturnParameter
    {
        get => _returnParameter;
        init => _returnParameter = Throw.IfNull(value);
    }

    /// <summary>Gets a JSON Schema describing the function and its input parameters.</summary>
    /// <remarks>
    /// <para>
    /// When specified, declares a self-contained JSON schema document that describes the function and its input parameters.
    /// A simple example of a JSON schema for a function that adds two numbers together is shown below:
    /// </para>
    /// <code>
    /// {
    ///   "title" : "addNumbers",
    ///   "description": "A simple function that adds two numbers together.",
    ///   "type": "object",
    ///   "properties": {
    ///     "a" : { "type": "number" },
    ///     "b" : { "type": "number", "default": 1 }
    ///   }, 
    ///   "required" : ["a"]
    /// }
    /// </code>
    /// <para>
    /// The metadata present in the schema document plays an important role in guiding AI function invocation.
    /// Functions should incorporate as much detail as possible. The arity of the "properties" keyword should
    /// also match the length of the <see cref="Parameters"/> list.
    /// </para>
    /// <para>
    /// When no schema is specified, consuming chat clients should assume the "{}" or "true" schema, indicating that any JSON input is admissible.
    /// </para>
    /// </remarks>
    public JsonElement Schema
    {
        get => _schema;
        init
        {
            AIJsonUtilities.ValidateSchemaDocument(value);
            _schema = value;
        }
    }

    /// <summary>Gets any additional properties associated with the function.</summary>
    public IReadOnlyDictionary<string, object?> AdditionalProperties
    {
        get => _additionalProperties;
        init => _additionalProperties = Throw.IfNull(value);
    }

    /// <summary>Gets a <see cref="JsonSerializerOptions"/> that can be used to marshal function parameters.</summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; init; }
}
