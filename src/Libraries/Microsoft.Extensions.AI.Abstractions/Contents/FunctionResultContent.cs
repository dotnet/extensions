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
    /// <param name="name">The function name that produced the result.</param>
    /// <param name="result">The function call result.</param>
    /// <param name="exception">Any exception that occurred when invoking the function.</param>
    [JsonConstructor]
    public FunctionResultContent(string callId, string name, object? result = null, Exception? exception = null)
    {
        CallId = Throw.IfNull(callId);
        Name = Throw.IfNull(name);
        Result = result;
        Exception = exception;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionResultContent"/> class.
    /// </summary>
    /// <param name="functionCall">The function call for which this is the result.</param>
    /// <param name="result">The function call result.</param>
    /// <param name="exception">Any exception that occurred when invoking the function.</param>
    public FunctionResultContent(FunctionCallContent functionCall, object? result = null, Exception? exception = null)
        : this(Throw.IfNull(functionCall).CallId, functionCall.Name, result, exception)
    {
    }

    /// <summary>
    /// Gets or sets the ID of the function call for which this is the result.
    /// </summary>
    /// <remarks>
    /// If this is the result for a <see cref="FunctionCallContent"/>, this should contain the same
    /// <see cref="FunctionCallContent.CallId"/> value.
    /// </remarks>
    public string CallId { get; set; }

    /// <summary>
    /// Gets or sets the name of the function that was called.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the result of the function call, or a generic error message if the function call failed.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets an exception that occurred if the function call failed.
    /// </summary>
    /// <remarks>
    /// When an instance of <see cref="FunctionResultContent"/> is serialized using <see cref="JsonSerializer"/>, any exception
    /// stored in this property will be serialized as a string. When deserialized, the string will be converted back to an instance
    /// of the base <see cref="Exception"/> type. As such, consumers shouldn't rely on the exact type of the exception stored in this property.
    /// </remarks>
    [JsonConverter(typeof(FunctionCallExceptionConverter))]
    public Exception? Exception { get; set; }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
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
