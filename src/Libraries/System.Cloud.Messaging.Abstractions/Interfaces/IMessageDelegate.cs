// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// The message delegate called by <see cref="IMessageMiddleware"/> to continue processing the message in the pipeline chain.
/// </summary>
/// <remarks>
/// It is inspired from the next delegate in the <see href="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware">ASP.NET Core Middleware</see> pipeline.
/// </remarks>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Analogous to ASP.NET Core RequestDelegate.")]
public interface IMessageDelegate
{
    /// <summary>
    /// Handles the message asynchronously.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = $"{nameof(MessageContext)} has {nameof(CancellationToken)}")]
    public ValueTask InvokeAsync(MessageContext context);
}
