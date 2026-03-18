// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted conversation.</summary>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
public class HostedConversation
{
    /// <summary>Gets or sets the conversation identifier.</summary>
    public string? ConversationId { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the raw representation of the conversation from the underlying provider.</summary>
    /// <remarks>
    /// If a <see cref="HostedConversation"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the conversation.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}
