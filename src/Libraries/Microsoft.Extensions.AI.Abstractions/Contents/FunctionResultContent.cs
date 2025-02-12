// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the result of a function call.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class FunctionResultContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionResultContent"/> class.
    /// </summary>
    /// <param name="callId">The function call ID for which this is the result.</param>
    /// <param name="result">
    /// <see langword="null"/> if the function returned <see langword="null"/> or was void-returning
    /// and thus had no result, or if the function call failed. Typically, however, to provide meaningfully representative
    /// information to an AI service, a human-readable representation of those conditions should be supplied.
    /// </param>
    [JsonConstructor]
    public FunctionResultContent(string callId, object? result)
    {
        CallId = Throw.IfNull(callId);
        Result = result;
    }

    /// <summary>
    /// Gets the ID of the function call for which this is the result.
    /// </summary>
    /// <remarks>
    /// If this is the result for a <see cref="FunctionCallContent"/>, this property should contain the same
    /// <see cref="FunctionCallContent.CallId"/> value.
    /// </remarks>
    public string CallId { get; }

    /// <summary>
    /// Gets or sets the result of the function call, or a generic error message if the function call failed.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> if the function returned <see langword="null"/> or was void-returning
    /// and thus had no result, or if the function call failed. Typically, however, to provide meaningfully representative
    /// information to an AI service, a human-readable representation of those conditions should be supplied.
    /// </remarks>
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets an exception that occurred if the function call failed.
    /// </summary>
    /// <remarks>
    /// This property is for informational purposes only. The <see cref="Exception"/> is not serialized as part of serializing
    /// instances of this class with <see cref="JsonSerializer"/>. As such, upon deserialization, this property will be <see langword="null"/>.
    /// Consumers should not rely on <see langword="null"/> indicating success.
    /// </remarks>
    [JsonIgnore]
    public Exception? Exception { get; set; }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string display = CallId is not null ?
                $"CallId = {CallId}, " :
                string.Empty;

            display += Exception is not null ?
                $"Error = {Exception.Message}" :
                $"Result = {Result?.ToString() ?? string.Empty}";

            return display;
        }
    }
}
