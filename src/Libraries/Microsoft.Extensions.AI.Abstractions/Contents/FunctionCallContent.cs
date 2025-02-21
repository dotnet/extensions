// Licensed to the .NET Foundation under one or more agreements.
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
        CallId = Throw.IfNull(callId);
        Name = Throw.IfNull(name);
        Arguments = arguments;
    }

    /// <summary>
    /// Gets the function call ID.
    /// </summary>
    public string CallId { get; }

    /// <summary>
    /// Gets the name of the function requested.
    /// </summary>
    public string Name { get; }

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

    /// <summary>
    /// Creates a new instance of <see cref="FunctionCallContent"/> parsing arguments using a specified encoding and parser.
    /// </summary>
    /// <typeparam name="TEncoding">The encoding format from which to parse function call arguments.</typeparam>
    /// <param name="encodedArguments">The input arguments encoded in <typeparamref name="TEncoding"/>.</param>
    /// <param name="callId">The function call ID.</param>
    /// <param name="name">The function name.</param>
    /// <param name="argumentParser">The parsing implementation converting the encoding to a dictionary of arguments.</param>
    /// <returns>A new instance of <see cref="FunctionCallContent"/> containing the parse result.</returns>
    public static FunctionCallContent CreateFromParsedArguments<TEncoding>(
        TEncoding encodedArguments,
        string callId,
        string name,
        Func<TEncoding, IDictionary<string, object?>?> argumentParser)
    {
        _ = Throw.IfNull(callId);
        _ = Throw.IfNull(name);
        _ = Throw.IfNull(encodedArguments);
        _ = Throw.IfNull(argumentParser);

        IDictionary<string, object?>? arguments = null;
        Exception? parsingException = null;

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            arguments = argumentParser(encodedArguments);
        }
        catch (Exception ex)
        {
            parsingException = new InvalidOperationException("Error parsing function call arguments.", ex);
        }
#pragma warning restore CA1031 // Do not catch general exception types

        return new FunctionCallContent(callId, name, arguments)
        {
            Exception = parsingException
        };
    }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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
