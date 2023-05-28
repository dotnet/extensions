// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// Feature interface for postponing the message processing.
/// </summary>
public interface IMessagePostponeFeature
{
    /// <summary>
    /// Postpones the message processing asynchronously.
    /// </summary>
    /// <param name="delay">The time span by which message processing is to be postponed.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    ValueTask PostponeAsync(TimeSpan delay, CancellationToken cancellationToken);
}
