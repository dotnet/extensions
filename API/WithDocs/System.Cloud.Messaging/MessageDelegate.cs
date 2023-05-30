// Assembly 'System.Cloud.Messaging'

using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// The message delegate called by <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> to continue processing the message in the pipeline chain.
/// </summary>
/// <remarks>
/// It is inspired from the next delegate in the <see href="https://learn.microsoft.com/aspnet/core/fundamentals/middleware">ASP.NET Core Middleware</see> pipeline.
/// </remarks>
/// <param name="context">The message context.</param>
/// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
public delegate ValueTask MessageDelegate(MessageContext context);
