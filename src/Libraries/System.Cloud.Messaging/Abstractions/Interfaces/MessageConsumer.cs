// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Base class implementation for consuming and processing messages.
/// </summary>
/// <remarks>
/// Implementation classes are recommended to override the <see cref="ProcessingStepAsync(CancellationToken)"/> method and execute the <see cref="FetchAndProcessMessageAsync(CancellationToken)"/>
/// in parallel or in any other way using Task Parallel Library (TPL) / DataFlow or any other abstractions.
/// </remarks>.
public abstract class MessageConsumer
{
    private bool _stopConsumer;

    /// <summary>
    /// Gets the underlying message source.
    /// </summary>
    protected IMessageSource MessageSource { get; }

    /// <summary>
    /// Gets the message delegate composed from the pipeline of <see cref="IMessageMiddleware"/> implementations and a terminal <see cref="MessageDelegate"/>.
    /// </summary>
    protected MessageDelegate PipelineDelegate { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageConsumer"/> class.
    /// </summary>
    /// <param name="messageSource"><see cref="MessageSource"/>.</param>
    /// <param name="messageMiddlewares">List of middleware in the async processing pipeline.</param>
    /// <param name="terminalDelegate">Terminal message delegate.</param>
    /// <param name="logger"><see cref="Logger"/>.</param>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    protected MessageConsumer(IMessageSource messageSource, IReadOnlyList<IMessageMiddleware> messageMiddlewares, MessageDelegate terminalDelegate, ILogger logger)
    {
        MessageSource = Throw.IfNull(messageSource);
        PipelineDelegate = MiddlewareUtils.ConstructPipelineDelegate(messageMiddlewares, terminalDelegate);
        Logger = Throw.IfNull(logger);
        _stopConsumer = false;
    }

    /// <summary>
    /// Start processing the messages.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to stop processing messages.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    public async virtual ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!_stopConsumer && !cancellationToken.IsCancellationRequested)
        {
            await ProcessingStepAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles failures that occur during the message processing.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="exception">The exception during the processing of the message.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = $"{nameof(MessageContext)} has {nameof(CancellationToken)}")]
    protected abstract ValueTask HandleMessageProcessingFailureAsync(MessageContext context, Exception exception);

    /// <summary>
    /// Handles the completion of the message processing.
    /// </summary>
    /// <remarks>Default behavior is to to not do anything and can be updated by the implementation class as per the requirement.</remarks>
    /// <param name="context">The message context.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = $"{nameof(MessageContext)} has {nameof(CancellationToken)}")]
    protected virtual ValueTask HandleMessageProcessingCompletionAsync(MessageContext context) => default;

    /// <summary>
    /// Represents processing steps for message(s).
    /// </summary>
    /// <remarks>
    /// Different implementation of the consumer can override this method and execute the <see cref="FetchAndProcessMessageAsync(CancellationToken)"/>
    /// in parallel or in any other way using Task Parallel Library (TPL) / DataFlow or any other abstractions.
    /// </remarks>
    /// <param name="cancellationToken">The cancellation token for the processing step.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    protected abstract ValueTask ProcessingStepAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Fetches a <see cref="MessageContext"/> via the <see cref="FetchMessageAsync(CancellationToken)"/> and processes it asynchronously via <see cref="ProcessMessageAsync(MessageContext)"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for retrieving and processing message.</param>
    /// <returns>Swallows any exception during retrieving or processing message and returns a non-faulted <see cref="ValueTask"/>.</returns>
    /// <exception cref="Exception">An exception is thrown during <see cref="HandleMessageProcessingFailureAsync(MessageContext, Exception)"/>.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = $"Handled by {nameof(Logger)}.")]
    protected virtual async ValueTask FetchAndProcessMessageAsync(CancellationToken cancellationToken)
    {
        MessageContext messageContext;
        try
        {
            messageContext = await FetchMessageAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception messageFetchException)
        {
            Log.MessageSourceFailedDuringReadingMessage(Logger, nameof(MessageSource), messageFetchException);
            return;
        }

        if (messageContext == null)
        {
            Log.MessageSourceReturnedNullMessageContext(Logger, nameof(MessageSource));
            return;
        }

        try
        {
            if (ShouldStopConsumer(messageContext))
            {
                _stopConsumer = true;
                return;
            }

            await ProcessMessageAsync(messageContext).ConfigureAwait(false);

            try
            {
                await HandleMessageProcessingCompletionAsync(messageContext).ConfigureAwait(false);
            }
            catch (Exception handlerException)
            {
                Log.ExceptionOccurredDuringHandlingMessageProcessingCompletion(Logger, handlerException);
            }
        }
        catch (Exception processingException)
        {
            try
            {
                await HandleMessageProcessingFailureAsync(messageContext, processingException).ConfigureAwait(false);
            }
            catch (Exception handlerException)
            {
                Log.ExceptionOccurredDuringHandlingMessageProcessingFailure(Logger, processingException, handlerException);
                throw;
            }
        }
        finally
        {
            try
            {
                ReleaseContext(messageContext);
            }
            catch (Exception releaseException)
            {
                Log.MessageSourceFailedDuringReleasingContext(Logger, nameof(MessageSource), releaseException);
            }
        }
    }

    /// <summary>
    /// Determines if the consumer should stop processing.
    /// </summary>
    /// <param name="messageContext">The message context.</param>
    /// <returns><see cref="bool"/> value indicating if consumer should stop processing.</returns>
    protected virtual bool ShouldStopConsumer(MessageContext messageContext) => false;

    /// <summary>
    /// Processes a message asynchronously.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = $"{nameof(MessageContext)} has {nameof(CancellationToken)}")]
    protected virtual ValueTask ProcessMessageAsync(MessageContext context)
    {
        _ = Throw.IfNull(context);
        return PipelineDelegate.Invoke(context);
    }

    /// <summary>
    /// Fetches message from the message source.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for fetching the message.</param>
    /// <returns><see cref="ValueTask"/> of nullable <see cref="MessageContext"/>.</returns>
    protected virtual ValueTask<MessageContext> FetchMessageAsync(CancellationToken cancellationToken) => MessageSource.ReadAsync(cancellationToken);

    /// <summary>
    /// Releases the message context.
    /// </summary>
    /// <param name="messageContext">The message context.</param>
    protected virtual void ReleaseContext(MessageContext messageContext) => MessageSource.Release(messageContext);
}
