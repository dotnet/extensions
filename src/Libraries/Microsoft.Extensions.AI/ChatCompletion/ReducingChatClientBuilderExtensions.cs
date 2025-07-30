// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for attaching a <see cref="ReducingChatClient"/> to a chat pipeline.
/// </summary>
[Experimental("MEAI001")]
public static class ReducingChatClientBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="ReducingChatClient"/> to the chat pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/> being used to build the chat pipeline.</param>
    /// <param name="reducer">The <see cref="IChatReducer"/> to apply to the chat client.</param>
    /// <returns>The configured <see cref="ChatClientBuilder"/> instance.</returns>
    public static ChatClientBuilder UseChatReducer(this ChatClientBuilder builder, IChatReducer reducer)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(reducer);

        return builder.Use(innerClient => new ReducingChatClient(innerClient, reducer));
    }
}
