// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Specifies the name to use for a parameter in the JSON schema generated for an <see cref="AIFunction"/>.
/// </summary>
/// <remarks>
/// By default, a parameter's name in an <see cref="AIFunction"/>'s <see cref="AIFunctionDeclaration.JsonSchema"/> is
/// taken from the .NET parameter name. Apply this attribute to use a different name.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
[Experimental(DiagnosticIds.Experiments.AIFunctionParameterName, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ParameterNameAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="ParameterNameAttribute"/> class.</summary>
    /// <param name="name">The name to use for the parameter in the generated JSON schema.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or composed entirely of whitespace.</exception>
    public ParameterNameAttribute(string name)
    {
        Name = Throw.IfNullOrWhitespace(name);
    }

    /// <summary>Gets the name to use for the parameter in the generated JSON schema.</summary>
    public string Name { get; }
}
