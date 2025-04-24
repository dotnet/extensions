// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// Extension methods for <see cref="ChatDetails"/>.
/// </summary>
public static class ChatDetailsExtensions
{
    /// <summary>
    /// Adds <see cref="ChatTurnDetails"/> for a particular LLM chat conversation turn to the
    /// <see cref="ChatDetails.TurnDetails"/> collection.
    /// </summary>
    /// <param name="chatDetails">
    /// The <see cref="ChatDetails"/> object to which the <paramref name="turnDetails"/> is to be added.
    /// </param>
    /// <param name="turnDetails">
    /// The <see cref="ChatTurnDetails"/> for a particular LLM chat conversation turn.
    /// </param>
    public static void AddTurnDetails(this ChatDetails chatDetails, ChatTurnDetails turnDetails)
    {
        _ = Throw.IfNull(chatDetails);

        chatDetails.TurnDetails.Add(turnDetails);
    }
}
