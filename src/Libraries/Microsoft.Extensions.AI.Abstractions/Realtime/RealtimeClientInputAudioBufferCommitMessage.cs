// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a real-time message for committing audio buffer input.
/// </summary>
[Experimental("MEAI001")]

public class RealtimeClientInputAudioBufferCommitMessage : RealtimeClientMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeClientInputAudioBufferCommitMessage"/> class.
    /// </summary>
    public RealtimeClientInputAudioBufferCommitMessage()
    {
    }
}

