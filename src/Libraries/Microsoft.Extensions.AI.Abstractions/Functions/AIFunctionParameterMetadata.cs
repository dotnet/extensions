// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides read-only metadata for an <see cref="AIFunction"/> parameter.
/// </summary>
public sealed class AIFunctionParameterMetadata
{
    private readonly string _name;

    /// <summary>Initializes a new instance of the <see cref="AIFunctionParameterMetadata"/> class for a parameter with the specified name.</summary>
    /// <param name="name">The name of the parameter.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="name"/> was null.</exception>
    /// <exception cref="ArgumentException">The <paramref name="name"/> was empty or composed entirely of whitespace.</exception>
    public AIFunctionParameterMetadata(string name)
    {
        _name = Throw.IfNullOrWhitespace(name);
    }

    /// <summary>Initializes a new instance of the <see cref="AIFunctionParameterMetadata"/> class as a copy of another <see cref="AIFunctionParameterMetadata"/>.</summary>
    /// <exception cref="ArgumentNullException">The <paramref name="metadata"/> was null.</exception>
    /// <remarks>This constructor creates a shallow clone of <paramref name="metadata"/>.</remarks>
    public AIFunctionParameterMetadata(AIFunctionParameterMetadata metadata)
    {
        _ = Throw.IfNull(metadata);
        _ = Throw.IfNullOrWhitespace(metadata.Name);

        _name = metadata.Name;

        Description = metadata.Description;
        HasDefaultValue = metadata.HasDefaultValue;
        DefaultValue = metadata.DefaultValue;
        IsRequired = metadata.IsRequired;
        ParameterType = metadata.ParameterType;
    }

    /// <summary>Gets the name of the parameter.</summary>
    public string Name
    {
        get => _name;
        init => _name = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>Gets a description of the parameter, suitable for use in describing the purpose to a model.</summary>
    public string? Description { get; init; }

    /// <summary>Gets a value indicating whether the parameter has a default value.</summary>
    public bool HasDefaultValue { get; init; }

    /// <summary>Gets the default value of the parameter.</summary>
    public object? DefaultValue { get; init; }

    /// <summary>Gets a value indicating whether the parameter is required.</summary>
    public bool IsRequired { get; init; }

    /// <summary>Gets the .NET type of the parameter.</summary>
    public Type? ParameterType { get; init; }
}
