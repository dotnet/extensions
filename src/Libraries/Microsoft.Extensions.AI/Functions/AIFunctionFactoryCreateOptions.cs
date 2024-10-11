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
/// Options that can be provided when creating an <see cref="AIFunction"/> from a method.
/// </summary>
public sealed class AIFunctionFactoryCreateOptions
{
    private JsonSerializerOptions _options = JsonDefaults.Options;

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

    /// <summary>Gets or sets the name to use for the function.</summary>
    /// <remarks>
    /// If <see langword="null"/>, it will default to one derived from the method represented by the passed <see cref="Delegate"/> or <see cref="MethodInfo"/>.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>Gets or sets the description to use for the function.</summary>
    /// <remarks>
    /// If <see langword="null"/>, it will default to one derived from the passed <see cref="Delegate"/> or <see cref="MethodInfo"/>, if possible
    /// (e.g. via a <see cref="DescriptionAttribute"/> on the method).
    /// </remarks>
    public string? Description { get; set; }

    /// <summary>Gets or sets metadata for the parameters of the function.</summary>
    /// <remarks>
    /// If <see langword="null"/>, it will default to metadata derived from the passed <see cref="Delegate"/> or <see cref="MethodInfo"/>.
    /// </remarks>
    public IReadOnlyList<AIFunctionParameterMetadata>? Parameters { get; set; }

    /// <summary>Gets or sets metadata for function's return parameter.</summary>
    /// <remarks>
    /// If <see langword="null"/>, it will default to one derived from the passed <see cref="Delegate"/> or <see cref="MethodInfo"/>.
    /// </remarks>
    public AIFunctionReturnParameterMetadata? ReturnParameter { get; set; }

    /// <summary>
    /// Gets or sets additional values that will be stored on the resulting <see cref="AIFunctionMetadata.AdditionalProperties" /> property.
    /// </summary>
    /// <remarks>
    /// This can be used to provide arbitrary information about the function.
    /// </remarks>
    public IReadOnlyDictionary<string, object?>? AdditionalProperties { get; set; }
}
