// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// Interface for a message source.
/// </summary>
public interface IMessageSource
{
    /// <summary>
    /// Reads message asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns><see cref="ValueTask{TResult}"/>.</returns>
    ValueTask<MessageContext> ReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Release the context.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    void Release(MessageContext context);
}
