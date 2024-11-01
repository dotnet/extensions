// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsChatClient"/> instances.</summary>
public static class ConfigureOptionsChatClientBuilderExtensions
{
    /// <summary>
    /// Adds a callback that updates or replaces <see cref="ChatOptions"/>. This can be used to set default options.
    /// </summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="configureOptions">
    /// The delegate to invoke to configure the <see cref="ChatOptions"/> instance. It is passed the caller-supplied <see cref="ChatOptions"/>
    /// instance and should return the configured <see cref="ChatOptions"/> instance to use.
    /// </param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <remarks>
    /// <para>
    /// The configuration callback is invoked with the caller-supplied <see cref="ChatOptions"/> instance. To override the caller-supplied options
    /// with a new instance, the callback may simply return that new instance, for example <c>_ => new ChatOptions() { MaxTokens = 1000 }</c>. To provide
    /// a new instance only if the caller-supplied instance is <see langword="null"/>, the callback may conditionally return a new instance, for example
    /// <c>options => options ?? new ChatOptions() { MaxTokens = 1000 }</c>. Any changes to the caller-provided options instance will persist on the
    /// original instance, so the callback must take care to only do so when such mutations are acceptable, such as by cloning the original instance
    /// and mutating the clone, for example:
    /// <c>
    /// options =>
    /// {
    ///     var newOptions = options?.Clone() ?? new();
    ///     newOptions.MaxTokens = 1000;
    ///     return newOptions;
    /// }
    /// </c>
    /// </para>
    /// <para>
    /// The callback may return <see langword="null"/>, in which case a <see langword="null"/> options will be passed to the next client in the pipeline.
    /// </para>
    /// </remarks>
    public static ChatClientBuilder UseChatOptions(
        this ChatClientBuilder builder, Func<ChatOptions?, ChatOptions?> configureOptions)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configureOptions);

        return builder.Use(innerClient => new ConfigureOptionsChatClient(innerClient, configureOptions));
    }
}
