// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// A class that records details related to all LLM chat conversation turns involved in the execution of a particular
/// <see cref="ScenarioRun"/>.
/// </summary>
public sealed class ChatDetails
{
#pragma warning disable CA2227
    // CA2227: Collection properties should be read only.
    // We disable this warning because we want this type to be fully mutable for serialization purposes and for general
    // convenience.

    /// <summary>
    /// Gets or sets the <see cref="ChatTurnDetails"/> for the LLM chat conversation turns recorded in this
    /// <see cref="ChatDetails"/> object.
    /// </summary>
    public IList<ChatTurnDetails> TurnDetails { get; set; }
#pragma warning restore CA2227

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatDetails"/> class.
    /// </summary>
    /// <param name="turnDetails">
    /// A list of <see cref="ChatTurnDetails"/> objects.
    /// </param>
    [JsonConstructor]
    public ChatDetails(IList<ChatTurnDetails> turnDetails)
    {
        TurnDetails = turnDetails;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatDetails"/> class.
    /// </summary>
    /// <param name="turnDetails">
    /// An enumeration of <see cref="ChatTurnDetails"/> objects.
    /// </param>
    public ChatDetails(IEnumerable<ChatTurnDetails> turnDetails)
        : this(turnDetails.ToList())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatDetails"/> class.
    /// </summary>
    /// <param name="turnDetails">
    /// An array of <see cref="ChatTurnDetails"/> objects.
    /// </param>
    public ChatDetails(params ChatTurnDetails[] turnDetails)
        : this(turnDetails as IEnumerable<ChatTurnDetails>)
    {
    }
}
