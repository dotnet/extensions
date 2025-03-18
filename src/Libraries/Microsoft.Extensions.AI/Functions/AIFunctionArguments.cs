// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Collections;

#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1112 // Closing parenthesis should be on line of opening parenthesis
#pragma warning disable SA1114 // Parameter list should follow declaration
#pragma warning disable CA1710 // Identifiers should have correct suffix

namespace Microsoft.Extensions.AI;

/// <summary>Represents arguments to be used with <see cref="AIFunction.InvokeAsync"/>.</summary>
/// <remarks>
/// <see cref="AIFunction.InvokeAsync"/> may be invoked with arbitary <see cref="IEnumerable{T}"/>
/// implementations. However, some <see cref="AIFunction"/> implementations may dynamically check
/// the type of the arguments and use the concrete type to perform more specific operations. By
/// checking for <see cref="AIFunctionArguments"/>, and implementation may optionally access
/// additional context provided, such as any <see cref="IServiceProvider"/> that may be associated
/// with the operation.
/// </remarks>
public class AIFunctionArguments : IReadOnlyDictionary<string, object?>
{
    private readonly IReadOnlyDictionary<string, object?> _arguments;

    /// <summary>Initializes a new instance of the <see cref="AIFunctionArguments"/> class.</summary>
    /// <param name="arguments">The arguments represented by this instance.</param>
    public AIFunctionArguments(IEnumerable<KeyValuePair<string, object?>>? arguments)
    {
        if (arguments is IReadOnlyDictionary<string, object?> irod)
        {
            _arguments = irod;
        }
        else if (arguments is null
#if NET
            || (Enumerable.TryGetNonEnumeratedCount(arguments, out int count) && count == 0)
#endif
            )
        {
            _arguments = EmptyReadOnlyDictionary<string, object?>.Instance;
        }
        else
        {
            _arguments = arguments.ToDictionary(
#if !NET
                x => x.Key, x => x.Value
#endif
                );
        }
    }

    /// <summary>Gets any services associated with these arguments.</summary>
    public IServiceProvider? Services { get; init; }

    /// <inheritdoc />
    public object? this[string key] => _arguments[key];

    /// <inheritdoc />
    public IEnumerable<string> Keys => _arguments.Keys;

    /// <inheritdoc />
    public IEnumerable<object?> Values => _arguments.Values;

    /// <inheritdoc />
    public int Count => _arguments.Count;

    /// <inheritdoc />
    public bool ContainsKey(string key) => _arguments.ContainsKey(key);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _arguments.GetEnumerator();

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value) => _arguments.TryGetValue(key, out value);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_arguments).GetEnumerator();
}
