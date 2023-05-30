// Assembly 'System.Cloud.Messaging'

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Cloud.Messaging;

/// <summary>
/// Provides default implementation to <see cref="T:System.Cloud.Messaging.MessageConsumer" />.
/// </summary>
public sealed class DefaultMessageConsumer : MessageConsumer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.Messaging.DefaultMessageConsumer" /> class.
    /// </summary>
    /// <param name="messageSource">The message source.</param>
    /// <param name="middlewares">The list of middlewares.</param>
    /// <param name="messageDelegate">The terminal message delegate.</param>
    /// <param name="logger">Logger.</param>
    public DefaultMessageConsumer(IMessageSource messageSource, IReadOnlyList<IMessageMiddleware> middlewares, MessageDelegate messageDelegate, ILogger logger);

    /// <summary>
    /// Rethrows exception that occurred during the message processing.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="exception">The exception during the processing of the message.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    protected override ValueTask HandleMessageProcessingFailureAsync(MessageContext context, Exception exception);

    /// <summary>
    /// Processes message one at a time.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    protected override ValueTask ProcessingStepAsync(CancellationToken cancellationToken);
}
