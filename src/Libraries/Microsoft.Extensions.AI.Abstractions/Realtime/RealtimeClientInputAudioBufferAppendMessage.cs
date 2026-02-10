// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time message for appending audio buffer input.
/// </summary>
[Experimental("MEAI001")]

public class RealtimeClientInputAudioBufferAppendMessage : RealtimeClientMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeClientInputAudioBufferAppendMessage"/> class.
    /// </summary>
    /// <param name="audioContent">The data content containing the audio buffer data to append.</param>
    public RealtimeClientInputAudioBufferAppendMessage(DataContent audioContent)
    {
        Content = audioContent;
    }

    /// <summary>
    /// Gets or sets the audio content to append to the model audio buffer.
    /// </summary>
    /// <remarks>
    /// The content should include the audio buffer data that needs to be appended to the input audio buffer.
    /// </remarks>
    public DataContent Content { get; set; }
}
