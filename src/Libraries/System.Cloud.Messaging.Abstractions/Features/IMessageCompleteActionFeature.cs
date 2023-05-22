// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// Feature interface for marking the message processing as complete.
/// </summary>
public interface IMessageCompleteActionFeature
{
    /// <summary>
    /// Marks the message processing to be completed asynchronously.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    ValueTask MarkCompleteAsync(CancellationToken cancellationToken);
}
