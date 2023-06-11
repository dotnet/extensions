// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// Interface for a middleware that uses <see cref="MessageContext"/> and the next <see cref="MessageDelegate"/> in the pipeline to process the message.
/// </summary>
/// <remarks>
/// Inspired from <see href="https://learn.microsoft.com/aspnet/core/fundamentals/middleware">ASP.NET Core Middleware</see>, which uses HttpContext and the next RequestDelegate in the pipeline.
/// </remarks>
public interface IMessageMiddleware
{
    /// <summary>
    /// Handles the message.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="nextHandler">The next handler in the async processing pipeline.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = $"{nameof(MessageContext)} has {nameof(CancellationToken)}")]
    ValueTask InvokeAsync(MessageContext context, MessageDelegate nextHandler);
}
