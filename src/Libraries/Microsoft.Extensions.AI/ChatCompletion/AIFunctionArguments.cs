// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1710 // Identifiers should have correct suffix

namespace Microsoft.Extensions.AI;

/// <summary>Represents arguments to be used with <see cref="AIFunction.InvokeAsync"/>.</summary>
/// <remarks>
/// <see cref="AIFunction.InvokeAsync"/> may be invoked with arbitary <see cref="IEnumerable{T}"/>
/// implementations. However, some <see cref="AIFunction"/> implementations may dynamically check
/// the type of the arguments, and if it's an <see cref="AIFunctionArguments"/>, use it to access
/// an <see cref="IServiceProvider"/> that's passed in separately from the arguments enumeration.
/// </remarks>
public class AIFunctionArguments : IEnumerable<KeyValuePair<string, object?>>
{
    /// <summary>The arguments represented by this instance.</summary>
    private readonly IEnumerable<KeyValuePair<string, object?>> _arguments;

    /// <summary>Initializes a new instance of the <see cref="AIFunctionArguments"/> class.</summary>
    /// <param name="arguments">The arguments represented by this instance.</param>
    /// <param name="serviceProvider">Options services associated with these arguments.</param>
    public AIFunctionArguments(IEnumerable<KeyValuePair<string, object?>>? arguments, IServiceProvider? serviceProvider = null)
    {
        _arguments = Throw.IfNull(arguments);
        ServiceProvider = serviceProvider;
    }

    /// <summary>Gets the services associated with these arguments.</summary>
    public IServiceProvider? ServiceProvider { get; }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _arguments.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_arguments).GetEnumerator();
}
