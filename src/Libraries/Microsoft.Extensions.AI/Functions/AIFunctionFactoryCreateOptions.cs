// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options that can be provided when creating an <see cref="AIFunction"/> from a method.
/// </summary>
public sealed class AIFunctionFactoryCreateOptions
{
    private JsonSerializerOptions _options = AIJsonUtilities.DefaultOptions;
    private AIJsonSchemaCreateOptions _schemaCreateOptions = AIJsonSchemaCreateOptions.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFunctionFactoryCreateOptions"/> class.
    /// </summary>
    public AIFunctionFactoryCreateOptions()
    {
    }

    /// <summary>Gets or sets the <see cref="JsonSerializerOptions"/> used to marshal .NET values being passed to the underlying delegate.</summary>
    public JsonSerializerOptions SerializerOptions
    {
        get => _options;
        set => _options = Throw.IfNull(value);
    }

    /// <summary>
    /// Gets or sets the <see cref="AIJsonSchemaCreateOptions"/> governing the generation of JSON schemas for the function.
    /// </summary>
    public AIJsonSchemaCreateOptions SchemaCreateOptions
    {
        get => _schemaCreateOptions;
        set => _schemaCreateOptions = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the name to use for the function.</summary>
    /// <value>
    /// The name to use for the function. The default value is a name derived from the method represented by the passed <see cref="Delegate"/> or <see cref="MethodInfo"/>.
    /// </value>
    public string? Name { get; set; }

    /// <summary>Gets or sets the description to use for the function.</summary>
    /// <value>
    /// The description for the function. The default value is a description derived from the passed <see cref="Delegate"/> or <see cref="MethodInfo"/>, if possible
    /// (for example, via a <see cref="DescriptionAttribute"/> on the method).
    /// </value>
    public string? Description { get; set; }

    /// <summary>Gets or sets metadata for the parameters of the function.</summary>
    /// <value>
    /// Metadata for the function's parameters. The default value is metadata derived from the passed <see cref="Delegate"/> or <see cref="MethodInfo"/>.
    /// </value>
    public IReadOnlyList<AIFunctionParameterMetadata>? Parameters { get; set; }

    /// <summary>Gets or sets metadata for function's return parameter.</summary>
    /// <value>
    /// Metadata for the function's return parameter. The default value is metadata derived from the passed <see cref="Delegate"/> or <see cref="MethodInfo"/>.
    /// </value>
    public AIFunctionReturnParameterMetadata? ReturnParameter { get; set; }

    /// <summary>
    /// Gets or sets additional values to store on the resulting <see cref="AIFunctionMetadata.AdditionalProperties" /> property.
    /// </summary>
    /// <remarks>
    /// This property can be used to provide arbitrary information about the function.
    /// </remarks>
    public IReadOnlyDictionary<string, object?>? AdditionalProperties { get; set; }
}
