// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// Interface for consuming and processing messages.
/// </summary>
public interface IMessageConsumer
{
    /// <summary>
    /// Start processing the messages.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> to stop processing messages.</param>
    /// <returns><see cref="Task"/>.</returns>
    ValueTask ExecuteAsync(CancellationToken cancellationToken);
}
