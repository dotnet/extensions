// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Represents the context for storing different <see cref="Features"/> during processing of message(s).
/// </summary>
/// <remarks>Inspired from <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext?view=aspnetcore-7.0">ASP.NET Core HttpContext</see>.</remarks>
public sealed class MessageContext
{
    /// <summary>
    /// Gets the <see cref="IFeatureCollection"/> for the message.
    /// </summary>
    public IFeatureCollection Features { get; }

    /// <summary>
    /// Gets or sets the <see cref="CancellationToken"/> for the cancelling the message processing.
    /// </summary>
    public CancellationToken MessageCancelledToken { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageContext"/> class.
    /// </summary>
    /// <param name="features"><see cref="Features"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the arguments is null.</exception>
    public MessageContext(IFeatureCollection features)
    {
        Features = Throw.IfNull(features);
        MessageCancelledToken = CancellationToken.None;
    }
}
