// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Specifies the name to use for an <see cref="AIFunction"/> or one of its parameters.
/// </summary>
/// <remarks>
/// The name is the identifier a model sees and uses to invoke a function or to supply an argument.
/// By default these names are inferred from .NET metadata: a function's name comes from the method name, and a
/// parameter's name comes from the .NET parameter name. Apply this attribute to use a different identifier.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
[Experimental(DiagnosticIds.Experiments.AIName, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class AINameAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="AINameAttribute"/> class.</summary>
    /// <param name="name">The name to use for the function or parameter.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or composed entirely of whitespace.</exception>
    public AINameAttribute(string name)
    {
        Name = Throw.IfNullOrWhitespace(name);
    }

    /// <summary>Gets the name to use for the function or parameter.</summary>
    public string Name { get; }
}
