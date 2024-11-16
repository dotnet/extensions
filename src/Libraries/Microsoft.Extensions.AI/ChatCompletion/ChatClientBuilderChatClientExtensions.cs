// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="IChatClient"/> in the context of <see cref="ChatClientBuilder"/>.</summary>
public static class ChatClientBuilderChatClientExtensions
{
    /// <summary>Creates a new <see cref="ChatClientBuilder"/> using <paramref name="innerClient"/> as its inner client.</summary>
    /// <param name="innerClient">The client to use as the inner client.</param>
    /// <returns>The new <see cref="ChatClientBuilder"/> instance.</returns>
    /// <remarks>
    /// This method is equivalent to using the <see cref="ChatClientBuilder"/> constructor directly,
    /// specifying <paramref name="innerClient"/> as the inner client.
    /// </remarks>
    public static ChatClientBuilder ToBuilder(this IChatClient innerClient)
    {
        _ = Throw.IfNull(innerClient);

        return new ChatClientBuilder(innerClient);
    }
}
