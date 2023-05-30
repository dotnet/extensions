// Assembly 'System.Cloud.Messaging'

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// Interface for a message source.
/// </summary>
public interface IMessageSource
{
    /// <summary>
    /// Reads message asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for reading message.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask`1" />.</returns>
    ValueTask<MessageContext> ReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Release the message context.
    /// </summary>
    /// <remarks>
    /// Allows pooling of the message context.
    /// </remarks>
    /// <param name="context"><see cref="T:System.Cloud.Messaging.MessageContext" />.</param>
    void Release(MessageContext context);
}
