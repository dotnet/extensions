// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Provides utility methods for <see cref="IMessageMiddleware"/>.
/// </summary>
internal static class MiddlewareUtils
{
    /// <summary>
    /// Construct pipeline delegate from the pipeline of <see cref="IReadOnlyList{IMessageMiddleware}"/> and terminal <see cref="MessageDelegate"/>.
    /// </summary>
    /// <param name="middlewares">List of middleware.</param>
    /// <param name="terminalDelegate">Terminal message delegate.</param>
    /// <returns>Pipeline delegate.</returns>
    public static MessageDelegate ConstructPipelineDelegate(IReadOnlyList<IMessageMiddleware> middlewares, MessageDelegate terminalDelegate)
    {
        _ = Throw.IfNull(terminalDelegate);

        if (middlewares == null || middlewares.Count == 0)
        {
            return terminalDelegate;
        }

        MessageDelegate pipelineDelegate = terminalDelegate;
        for (int i = middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = middlewares[i];
            pipelineDelegate = middleware.Stitch(pipelineDelegate);
        }

        return pipelineDelegate;
    }
}
