// Assembly 'System.Cloud.Messaging'

using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// Interface for a middleware that uses <see cref="T:System.Cloud.Messaging.MessageContext" /> and the next <see cref="T:System.Cloud.Messaging.MessageDelegate" /> in the pipeline to process the message.
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
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    ValueTask InvokeAsync(MessageContext context, MessageDelegate nextHandler);
}
