// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a client message that requests updating the session configuration.
/// </summary>
/// <remarks>
/// <para>
/// Sending this message requests that the provider update the active session with new options.
/// Not all providers support mid-session updates. Providers that do not support this message
/// may ignore it or throw a <see cref="System.NotSupportedException"/>.
/// </para>
/// <para>
/// When a provider processes this message, it should update its <see cref="IRealtimeClientSession.Options"/>
/// property to reflect the new configuration.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public class SessionUpdateRealtimeClientMessage : RealtimeClientMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionUpdateRealtimeClientMessage"/> class.
    /// </summary>
    /// <param name="options">The session options to apply.</param>
    public SessionUpdateRealtimeClientMessage(RealtimeSessionOptions options)
    {
        Options = Throw.IfNull(options);
    }

    /// <summary>
    /// Gets or sets the session options to apply.
    /// </summary>
    public RealtimeSessionOptions Options { get; set; }
}
