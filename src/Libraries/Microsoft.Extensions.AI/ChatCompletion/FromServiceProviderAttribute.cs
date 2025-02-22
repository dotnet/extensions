// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>Indicates that a parameter to an <see cref="AIFunction"/> should be sourced from an associated <see cref="IServiceProvider"/>.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromServiceProviderAttribute : SkipJsonFunctionSchemaParameterAttribute
{
    /// <summary>Initializes a new instance of the <see cref="FromServiceProviderAttribute"/> class.</summary>
    /// <param name="serviceKey">Optional key to use when resolving the service.</param>
    public FromServiceProviderAttribute(object? serviceKey = null)
    {
        ServiceKey = serviceKey;
    }

    /// <summary>Gets the key to use when resolving the service.</summary>
    public object? ServiceKey { get; }
}
