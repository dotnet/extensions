// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides read-only metadata for an <see cref="AIFunction"/>'s return parameter.
/// </summary>
public sealed class AIFunctionReturnParameterMetadata
{
    /// <summary>Gets an empty return parameter metadata instance.</summary>
    public static AIFunctionReturnParameterMetadata Empty { get; } = new();

    /// <summary>The JSON schema describing the function and its input parameters.</summary>
    private readonly JsonElement _schema = AIJsonUtilities.DefaultJsonSchema;

    /// <summary>Initializes a new instance of the <see cref="AIFunctionReturnParameterMetadata"/> class.</summary>
    public AIFunctionReturnParameterMetadata()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AIFunctionReturnParameterMetadata"/> class as a copy of another <see cref="AIFunctionReturnParameterMetadata"/>.</summary>
    public AIFunctionReturnParameterMetadata(AIFunctionReturnParameterMetadata metadata)
    {
        Description = Throw.IfNull(metadata).Description;
        ParameterType = metadata.ParameterType;
        Schema = metadata.Schema;
    }

    /// <summary>Gets a description of the return parameter, suitable for use in describing the purpose to a model.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the .NET type of the return parameter.</summary>
    public Type? ParameterType { get; init; }

    /// <summary>Gets a JSON Schema describing the type of the return parameter.</summary>
    public JsonElement Schema
    {
        get => _schema;
        init
        {
            AIJsonUtilities.ValidateSchemaDocument(value);
            _schema = value;
        }
    }
}
