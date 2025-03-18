// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>Indicates that a parameter to an <see cref="AIFunction"/> should be sourced from an associated <see cref="IServiceProvider"/>.</summary>
/// <remarks>
/// <see cref="AIFunctionFactory"/> uses this attribute to determine whether a parameter's value should be sourced from
/// an <see cref="IServiceProvider"/> instead of from the nominal arguments passed to the function. The <see cref="IServiceProvider"/>
/// is extracted from the <see cref="AIFunctionArguments.Services"/> property of an <see cref="AIFunctionArguments"/> passed
/// as the arguments to a call to <see cref="AIFunction.InvokeAsync"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromServicesAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="FromServicesAttribute"/> class.</summary>
    public FromServicesAttribute()
    {
    }
}
