// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for a hosted conversation client request.</summary>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
public class HostedConversationClientOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedConversationClientOptions"/> class.
    /// </summary>
    public HostedConversationClientOptions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedConversationClientOptions"/> class
    /// by cloning the properties of another instance.
    /// </summary>
    /// <param name="other">The instance to clone.</param>
    protected HostedConversationClientOptions(HostedConversationClientOptions? other)
    {
        if (other is null)
        {
            return;
        }

        Limit = other.Limit;
        RawRepresentationFactory = other.RawRepresentationFactory;
        AdditionalProperties = other.AdditionalProperties?.Clone();
    }

    /// <summary>Gets or sets the maximum number of items to return in a list operation.</summary>
    /// <remarks>
    /// If not specified, the provider's default limit will be used.
    /// </remarks>
    public int? Limit { get; set; }

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the conversation client options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="IHostedConversationClient" /> implementation may have its own representation of options.
    /// When an operation is invoked with a <see cref="HostedConversationClientOptions" />, that implementation may convert
    /// the provided options into its own representation in order to use it while performing the operation.
    /// For situations where a consumer knows which concrete <see cref="IHostedConversationClient" /> is being used
    /// and how it represents options, a new instance of that implementation-specific options type may be returned
    /// by this callback, for the <see cref="IHostedConversationClient" /> implementation to use instead of creating a new
    /// instance. Such implementations may mutate the supplied options instance further based on other settings
    /// supplied on this <see cref="HostedConversationClientOptions" /> instance or from other inputs,
    /// therefore, it is <b>strongly recommended</b> to not return shared instances and instead make the callback
    /// return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly typed
    /// properties on <see cref="HostedConversationClientOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<IHostedConversationClient, object?>? RawRepresentationFactory { get; set; }

    /// <summary>Gets or sets additional properties for the request.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Creates a shallow clone of the current <see cref="HostedConversationClientOptions"/> instance.</summary>
    /// <returns>A shallow clone of the current <see cref="HostedConversationClientOptions"/> instance.</returns>
    public virtual HostedConversationClientOptions Clone() => new(this);
}
