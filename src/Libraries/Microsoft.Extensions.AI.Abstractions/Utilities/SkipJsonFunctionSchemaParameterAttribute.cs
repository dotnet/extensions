// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#pragma warning disable CA1813 // Avoid unsealed attributes

namespace Microsoft.Extensions.AI;

/// <summary>Indicates that a parameter to a method should not be included in a generated JSON schema by <see cref="AIJsonUtilities.CreateFunctionJsonSchema"/>.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class SkipJsonFunctionSchemaParameterAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="SkipJsonFunctionSchemaParameterAttribute"/> class.</summary>
    public SkipJsonFunctionSchemaParameterAttribute()
    {
    }
}
