// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Builder extensions for <see cref="ToolGroupingChatClient"/>.</summary>
[Experimental("MEAI001")]
public static class ChatClientBuilderToolGroupingExtensions
{
    /// <summary>Adds tool grouping middleware to the pipeline.</summary>
    /// <param name="builder">Chat client builder.</param>
    /// <param name="configure">Configuration delegate.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>Should appear before tool reduction and function invocation middleware.</remarks>
    public static ChatClientBuilder UseToolGrouping(this ChatClientBuilder builder, Action<ToolGroupingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);
        var opts = new ToolGroupingOptions();
        configure(opts);
        return builder.Use(inner => new ToolGroupingChatClient(inner, opts));
    }
}
