// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// Interface for writing message to a destination.
/// </summary>
public interface IMessageDestination
{
    /// <summary>
    /// Write message asynchronously.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = $"{nameof(MessageContext)} has {nameof(CancellationToken)}")]
    public ValueTask WriteAsync(MessageContext context);
}
