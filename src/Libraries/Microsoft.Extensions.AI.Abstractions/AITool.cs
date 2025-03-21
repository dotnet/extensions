// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Shared.Collections;

namespace Microsoft.Extensions.AI;

#pragma warning disable S1694 // An abstract class should have both abstract and concrete methods

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
