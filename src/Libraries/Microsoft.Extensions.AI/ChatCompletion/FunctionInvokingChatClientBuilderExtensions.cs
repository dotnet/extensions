// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging function invocations.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="FunctionInvokingChatClient"/> instance.</param>
    /// <returns>The supplied <paramref name="builder"/>.</returns>
    public static ChatClientBuilder UseFunctionInvocation(
        this ChatClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        Action<FunctionInvokingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((services, innerClient) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var chatClient = new FunctionInvokingChatClient(innerClient, loggerFactory?.CreateLogger(typeof(FunctionInvokingChatClient)));
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}
