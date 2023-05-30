// Assembly 'System.Cloud.Messaging'

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Cloud.Messaging;

public abstract class MessageConsumer
{
    protected IMessageSource MessageSource { get; }
    protected MessageDelegate PipelineDelegate { get; }
    protected ILogger Logger { get; }
    protected MessageConsumer(IMessageSource messageSource, IReadOnlyList<IMessageMiddleware> messageMiddlewares, MessageDelegate terminalDelegate, ILogger logger);
    public virtual ValueTask ExecuteAsync(CancellationToken cancellationToken);
    protected abstract ValueTask HandleMessageProcessingFailureAsync(MessageContext context, Exception exception);
    protected virtual ValueTask HandleMessageProcessingCompletionAsync(MessageContext context);
    protected abstract ValueTask ProcessingStepAsync(CancellationToken cancellationToken);
    protected virtual ValueTask FetchAndProcessMessageAsync(CancellationToken cancellationToken);
    protected virtual bool ShouldStopConsumer(MessageContext messageContext);
    protected virtual ValueTask ProcessMessageAsync(MessageContext context);
    protected virtual ValueTask<MessageContext> FetchMessageAsync(CancellationToken cancellationToken);
    protected virtual void ReleaseContext(MessageContext messageContext);
}
