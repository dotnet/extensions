// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// Extension methods for <see cref="ChatDetails"/>.
/// </summary>
public static class ChatDetailsExtensions
{
    /// <summary>
    /// Adds <see cref="ChatTurnDetails"/> for one or more LLM chat conversation turns to the
    /// <see cref="ChatDetails.TurnDetails"/> collection.
    /// </summary>
    /// <param name="chatDetails">
    /// The <see cref="ChatDetails"/> object to which the <paramref name="turnDetails"/> are to be added.
    /// </param>
    /// <param name="turnDetails">
    /// The <see cref="ChatTurnDetails"/> for one or more LLM chat conversation turns.
    /// </param>
    public static void AddTurnDetails(this ChatDetails chatDetails, IEnumerable<ChatTurnDetails> turnDetails)
    {
        _ = Throw.IfNull(chatDetails);
        _ = Throw.IfNull(turnDetails);

        foreach (ChatTurnDetails t in turnDetails)
        {
            chatDetails.TurnDetails.Add(t);
        }
    }

    /// <summary>
    /// Adds <see cref="ChatTurnDetails"/> for one or more LLM chat conversation turns to the
    /// <see cref="ChatDetails.TurnDetails"/> collection.
    /// </summary>
    /// <param name="chatDetails">
    /// The <see cref="ChatDetails"/> object to which the <paramref name="turnDetails"/> are to be added.
    /// </param>
    /// <param name="turnDetails">
    /// The <see cref="ChatTurnDetails"/> for one or more LLM chat conversation turns.
    /// </param>
    public static void AddTurnDetails(this ChatDetails chatDetails, params ChatTurnDetails[] turnDetails)
        => chatDetails.AddTurnDetails(turnDetails as IEnumerable<ChatTurnDetails>);
}
