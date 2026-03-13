// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for creating a hosted conversation.</summary>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
public class HostedConversationCreationOptions
{
    /// <summary>Initializes a new instance of the <see cref="HostedConversationCreationOptions"/> class.</summary>
    public HostedConversationCreationOptions()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HostedConversationCreationOptions"/> class, performing a shallow copy of all properties from <paramref name="other"/>.</summary>
    protected HostedConversationCreationOptions(HostedConversationCreationOptions? other)
    {
        if (other is null)
        {
            return;
        }

        AdditionalProperties = other.AdditionalProperties?.Clone();
        Metadata = other.Metadata is not null ? new(other.Metadata) : null;
        RawRepresentationFactory = other.RawRepresentationFactory;

        if (other.Messages is not null)
        {
            Messages = [.. other.Messages];
        }
    }

    /// <summary>Gets or sets metadata to associate with the conversation.</summary>
    public AdditionalPropertiesDictionary<string>? Metadata { get; set; }

    /// <summary>Gets or sets initial messages to populate the conversation.</summary>
    public IList<ChatMessage>? Messages { get; set; }

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the conversation creation options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="IHostedConversationClient" /> implementation may have its own representation of options.
    /// When <see cref="IHostedConversationClient" /> operations are invoked with a <see cref="HostedConversationCreationOptions" />,
    /// that implementation may convert the provided options into its own representation in order to use it while performing the operation.
    /// For situations where a consumer knows which concrete <see cref="IHostedConversationClient" /> is being used and how it represents options,
    /// a new instance of that implementation-specific options type may be returned by this callback, for the <see cref="IHostedConversationClient" />
    /// implementation to use instead of creating a new instance. Such implementations may mutate the supplied options
    /// instance further based on other settings supplied on this <see cref="HostedConversationCreationOptions" /> instance or from other inputs,
    /// therefore, it is <b>strongly recommended</b> to not return shared instances and instead make the callback return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly typed
    /// properties on <see cref="HostedConversationCreationOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<IHostedConversationClient, object?>? RawRepresentationFactory { get; set; }

    /// <summary>Gets or sets any additional properties associated with the options.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Produces a clone of the current <see cref="HostedConversationCreationOptions"/> instance.</summary>
    /// <returns>A clone of the current <see cref="HostedConversationCreationOptions"/> instance.</returns>
    public virtual HostedConversationCreationOptions Clone() => new(this);
}
