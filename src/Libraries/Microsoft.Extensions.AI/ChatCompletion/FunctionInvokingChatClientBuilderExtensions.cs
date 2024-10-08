// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for attaching a <see cref="FunctionInvokingChatClient"/> to a chat pipeline.
/// </summary>
public static class FunctionInvokingChatClientBuilderExtensions
{
    /// <summary>
    /// Enables automatic function call invocation on the chat pipeline.
    /// </summary>
    /// <remarks>This works by adding an instance of <see cref="FunctionInvokingChatClient"/> with default options.</remarks>
    /// <param name="builder">The <see cref="ChatClientBuilder"/> being used to build the chat pipeline.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="FunctionInvokingChatClient"/> instance.</param>
    /// <returns>The supplied <paramref name="builder"/>.</returns>
    public static ChatClientBuilder UseFunctionInvocation(this ChatClientBuilder builder, Action<FunctionInvokingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use(innerClient =>
        {
            var chatClient = new FunctionInvokingChatClient(innerClient);
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}
