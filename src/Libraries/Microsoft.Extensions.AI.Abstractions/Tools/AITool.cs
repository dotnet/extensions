// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Shared.Collections;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a tool that can be specified to an AI service.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public abstract class AITool
{
    /// <summary>Initializes a new instance of the <see cref="AITool"/> class.</summary>
    protected AITool()
    {
    }

    /// <summary>Gets the name of the tool.</summary>
    public virtual string Name => GetType().Name;

    /// <summary>Gets a description of the tool, suitable for use in describing the purpose to a model.</summary>
    public virtual string Description => string.Empty;

    /// <summary>Gets any additional properties associated with the tool.</summary>
    public virtual IReadOnlyDictionary<string, object?> AdditionalProperties => EmptyReadOnlyDictionary<string, object?>.Instance;

    /// <inheritdoc/>
    public override string ToString() => Name;

    /// <summary>Asks the <see cref="AITool"/> for an object of the specified type <paramref name="serviceType"/>.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly-typed services that might be provided by the <see cref="AITool"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public virtual object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <summary>Asks the <see cref="AITool"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that may be provided by the <see cref="AITool"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public TService? GetService<TService>(object? serviceKey = null) =>
        GetService(typeof(TService), serviceKey) is TService service ? service : default;

    /// <summary>Gets the string to display in the debugger for this instance.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            StringBuilder sb = new(Name);

            if (Description is string description && !string.IsNullOrEmpty(description))
            {
                _ = sb.Append(" (").Append(description).Append(')');
            }

            foreach (var entry in AdditionalProperties)
            {
                _ = sb.Append(", ").Append(entry.Key).Append(" = ").Append(entry.Value);
            }

            return sb.ToString();
        }
    }
}
