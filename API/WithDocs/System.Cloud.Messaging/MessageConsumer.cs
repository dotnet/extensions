// Assembly 'System.Cloud.Messaging'

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Cloud.Messaging;

/// <summary>
/// Base class implementation for consuming and processing messages.
/// </summary>
/// <remarks>
/// Implementation classes are recommended to override the <see cref="M:System.Cloud.Messaging.MessageConsumer.ProcessingStepAsync(System.Threading.CancellationToken)" /> method and execute the <see cref="M:System.Cloud.Messaging.MessageConsumer.FetchAndProcessMessageAsync(System.Threading.CancellationToken)" />
/// in parallel or in any other way using Task Parallel Library (TPL) / DataFlow or any other abstractions.
/// </remarks>.
public abstract class MessageConsumer
{
    /// <summary>
    /// Gets the underlying message source.
    /// </summary>
    protected IMessageSource MessageSource { get; }

    /// <summary>
    /// Gets the message delegate composed from the pipeline of <see cref="T:System.Cloud.Messaging.IMessageMiddleware" /> implementations and a terminal <see cref="T:System.Cloud.Messaging.MessageDelegate" />.
    /// </summary>
    protected MessageDelegate PipelineDelegate { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.Messaging.MessageConsumer" /> class.
    /// </summary>
    /// <param name="messageSource"><see cref="P:System.Cloud.Messaging.MessageConsumer.MessageSource" />.</param>
    /// <param name="messageMiddlewares">List of middleware in the async processing pipeline.</param>
    /// <param name="terminalDelegate">Terminal message delegate.</param>
    /// <param name="logger"><see cref="P:System.Cloud.Messaging.MessageConsumer.Logger" />.</param>
    /// <exception cref="T:System.ArgumentNullException">Any argument is <see langword="null" />.</exception>
    protected MessageConsumer(IMessageSource messageSource, IReadOnlyList<IMessageMiddleware> messageMiddlewares, MessageDelegate terminalDelegate, ILogger logger);

    /// <summary>
    /// Start processing the messages.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to stop processing messages.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    public virtual ValueTask ExecuteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Handles failures that occur during the message processing.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="exception">The exception during the processing of the message.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    protected abstract ValueTask HandleMessageProcessingFailureAsync(MessageContext context, Exception exception);

    /// <summary>
    /// Handles the completion of the message processing.
    /// </summary>
    /// <remarks>Default behavior is to to not do anything and can be updated by the implementation class as per the requirement.</remarks>
    /// <param name="context">The message context.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    protected virtual ValueTask HandleMessageProcessingCompletionAsync(MessageContext context);

    /// <summary>
    /// Represents processing steps for message(s).
    /// </summary>
    /// <remarks>
    /// Different implementation of the consumer can override this method and execute the <see cref="M:System.Cloud.Messaging.MessageConsumer.FetchAndProcessMessageAsync(System.Threading.CancellationToken)" />
    /// in parallel or in any other way using Task Parallel Library (TPL) / DataFlow or any other abstractions.
    /// </remarks>
    /// <param name="cancellationToken">The cancellation token for the processing step.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    protected abstract ValueTask ProcessingStepAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Fetches a <see cref="T:System.Cloud.Messaging.MessageContext" /> via the <see cref="M:System.Cloud.Messaging.MessageConsumer.FetchMessageAsync(System.Threading.CancellationToken)" /> and processes it asynchronously via <see cref="M:System.Cloud.Messaging.MessageConsumer.ProcessMessageAsync(System.Cloud.Messaging.MessageContext)" />.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for retrieving and processing message.</param>
    /// <returns>Swallows any exception during retrieving or processing message and returns a non-faulted <see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    /// <exception cref="T:System.Exception">An exception is thrown during <see cref="M:System.Cloud.Messaging.MessageConsumer.HandleMessageProcessingFailureAsync(System.Cloud.Messaging.MessageContext,System.Exception)" />.</exception>
    protected virtual ValueTask FetchAndProcessMessageAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Determines if the consumer should stop processing.
    /// </summary>
    /// <param name="messageContext">The message context.</param>
    /// <returns><see cref="T:System.Boolean" /> value indicating if consumer should stop processing.</returns>
    protected virtual bool ShouldStopConsumer(MessageContext messageContext);

    /// <summary>
    /// Processes a message asynchronously.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    protected virtual ValueTask ProcessMessageAsync(MessageContext context);

    /// <summary>
    /// Fetches message from the message source.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for fetching the message.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" /> of nullable <see cref="T:System.Cloud.Messaging.MessageContext" />.</returns>
    protected virtual ValueTask<MessageContext> FetchMessageAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Releases the message context.
    /// </summary>
    /// <param name="messageContext">The message context.</param>
    protected virtual void ReleaseContext(MessageContext messageContext);
}
