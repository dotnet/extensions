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
    /// Adds a callback that configures a <see cref="ChatOptions"/> to be passed to the next client in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="ChatOptions"/> instance.
    /// It is passed a clone of the caller-supplied <see cref="ChatOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// This can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="ChatOptions"/> if the caller didn't supply a <see cref="ChatOptions"/> instance, or a clone (via <see cref="ChatOptions.Clone"/>
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ChatClientBuilder ConfigureOptions(
        this ChatClientBuilder builder, Action<ChatOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerClient => new ConfigureOptionsChatClient(innerClient, configure));
    }
}
