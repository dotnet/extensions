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
    private string _name = string.Empty;

    /// <summary>The description of the function.</summary>
    private string _description = string.Empty;

    /// <summary>The function's parameters.</summary>
    private IReadOnlyList<AIFunctionParameterMetadata> _parameters = [];

    /// <summary>The function's return parameter.</summary>
    private AIFunctionReturnParameterMetadata _returnParameter = AIFunctionReturnParameterMetadata.Empty;

    /// <summary>Optional additional properties in addition to the named properties already available on this class.</summary>
    private IReadOnlyDictionary<string, object?> _additionalProperties = EmptyReadOnlyDictionary<string, object?>.Instance;

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
    /// <remarks>If the function has no parameters, the returned list will be empty.</remarks>
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
    /// <remarks>If the function has no return parameter, the returned value will be a default instance of a <see cref="AIFunctionReturnParameterMetadata"/>.</remarks>
    public AIFunctionReturnParameterMetadata ReturnParameter
    {
        get => _returnParameter;
        init => _returnParameter = Throw.IfNull(value);
    }

    /// <summary>Gets any additional properties associated with the function.</summary>
    public IReadOnlyDictionary<string, object?> AdditionalProperties
    {
        get => _additionalProperties;
        init => _additionalProperties = Throw.IfNull(value);
    }

    /// <summary>Gets a <see cref="JsonSerializerOptions"/> that may be used to marshal function parameters.</summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; init; }
}
