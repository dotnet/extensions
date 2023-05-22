// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace System.Cloud.Messaging;

/// <summary>
/// Reference base class implementation for <see cref="IMessageConsumer"/>.
/// </summary>
public class BaseMessageConsumer : IMessageConsumer
{
    /// <summary>
    /// Gets the underlying <see cref="IMessageSource"/>.
    /// </summary>
    protected IMessageSource MessageSource { get; }

    /// <summary>
    /// Gets the <see cref="IMessageDelegate"/>.
    /// </summary>
    protected IMessageDelegate MessageDelegate { get; }

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseMessageConsumer"/> class.
    /// </summary>
    /// <param name="messageSource"><see cref="IMessageSource"/>.</param>
    /// <param name="messageDelegate"><see cref="IMessageDelegate"/>.</param>
    /// <param name="logger"><see cref="ILogger"/>.</param>
    protected BaseMessageConsumer(IMessageSource messageSource, IMessageDelegate messageDelegate, ILogger logger)
    {
        MessageSource = Throw.IfNull(messageSource);
        MessageDelegate = Throw.IfNull(messageDelegate);
        Logger = Throw.IfNull(logger);
    }

    /// <inheritdoc/>
    public async virtual ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await ProcessingStepAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles exception occured during processing message.
    /// </summary>
    /// <remarks>Default behaviour is to rethrow the exception.</remarks>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="exception"><see cref="Exception"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = $"{nameof(MessageContext)} has {nameof(CancellationToken)}")]
    protected virtual ValueTask OnMessageProcessingFailureAsync(MessageContext context, Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
        return default;
    }

    /// <summary>
    /// Handles the message processing completion.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = $"{nameof(MessageContext)} has {nameof(CancellationToken)}")]
    protected virtual ValueTask OnMessageProcessingCompletionAsync(MessageContext context) => default;

    /// <summary>
    /// Represents processing steps for message(s).
    /// </summary>
    /// <remarks>
    /// Different implementation of the consumer can override this method and execute the <see cref="FetchAndProcessMessageAsync(CancellationToken)"/>
    /// in parallel or in any other way using Task Parallel Library (TPL) / DataFlow or any other abstractions.
    /// </remarks>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    protected virtual async ValueTask ProcessingStepAsync(CancellationToken cancellationToken)
    {
        await FetchAndProcessMessageAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Process a single message asynchronously.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
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
            await ProcessMessageAsync(messageContext).ConfigureAwait(false);

            try
            {
                await OnMessageProcessingCompletionAsync(messageContext).ConfigureAwait(false);
            }
            catch (Exception handlerException)
            {
                Log.ExceptionOccuredDuringHandlingMessageProcessingCompletion(Logger, handlerException);
            }
        }
        catch (Exception processingException)
        {
            try
            {
                await OnMessageProcessingFailureAsync(messageContext, processingException).ConfigureAwait(false);
            }
            catch (Exception handlerException)
            {
                Log.ExceptionOccuredDuringHandlingMessageProcessingFailure(Logger, processingException, handlerException);
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
    /// Process a message asynchronously.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    [SuppressMessage("Resilience", "R9A061:The async method doesn't support cancellation", Justification = $"{nameof(MessageContext)} has {nameof(CancellationToken)}")]
    protected virtual async ValueTask ProcessMessageAsync(MessageContext context)
    {
        _ = Throw.IfNull(context);

        await MessageDelegate.InvokeAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetch message from message source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns><see cref="ValueTask"/> of nullable <see cref="MessageContext"/>.</returns>
    protected virtual async ValueTask<MessageContext> FetchMessageAsync(CancellationToken cancellationToken)
    {
        MessageContext message = await MessageSource.ReadAsync(cancellationToken).ConfigureAwait(false);
        return message;
    }

    /// <summary>
    /// Release context.
    /// </summary>
    /// <param name="messageContext"><see cref="MessageContext"/>.</param>
    protected virtual void ReleaseContext(MessageContext messageContext) => MessageSource.Release(messageContext);
}
