// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a reducer capable of shrinking the size of a list of chat messages.
/// </summary>
[Experimental("MEAI001")]
public interface IChatReducer
{
    /// <summary>Reduces the size of a list of chat messages.</summary>
    /// <param name="messages">The messages to reduce.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The new list of messages.</returns>
    Task<IList<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken);
}
