﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a function call request.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class FunctionCallContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionCallContent"/> class.
    /// </summary>
    /// <param name="callId">The function call ID.</param>
    /// <param name="name">The function name.</param>
    /// <param name="arguments">The function original arguments.</param>
    [JsonConstructor]
    public FunctionCallContent(string callId, string name, IDictionary<string, object?>? arguments = null)
    {
        Name = Throw.IfNull(name);
        CallId = callId;
        Arguments = arguments;
    }

    /// <summary>
    /// Gets or sets the function call ID.
    /// </summary>
    public string CallId { get; set; }

    /// <summary>
    /// Gets or sets the name of the function requested.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the arguments requested to be provided to the function.
    /// </summary>
    public IDictionary<string, object?>? Arguments { get; set; }

    /// <summary>
    /// Gets or sets any exception that occurred while mapping the original function call data to this class.
    /// </summary>
    /// <remarks>
    /// This property is for information purposes only. The <see cref="Exception"/> is not serialized as part of serializing
    /// instances of this class with <see cref="JsonSerializer"/>; as such, upon deserialization, this property will be <see langword="null"/>.
    /// Consumers should not rely on <see langword="null"/> indicating success. 
    /// </remarks>
    [JsonIgnore]
    public Exception? Exception { get; set; }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    private string DebuggerDisplay
    {
        get
        {
            string display = CallId is not null ?
                $"CallId = {CallId}, " :
                string.Empty;

            display += Arguments is not null ?
                $"Call = {Name}({string.Join(", ", Arguments)})" :
                $"Call = {Name}()";

            return display;
        }
    }
}
