// Assembly 'System.Cloud.Messaging'

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace System.Cloud.Messaging;

public sealed class DefaultMessageConsumer : MessageConsumer
{
    public DefaultMessageConsumer(IMessageSource messageSource, IReadOnlyList<IMessageMiddleware> middlewares, MessageDelegate messageDelegate, ILogger logger);
    protected override ValueTask HandleMessageProcessingFailureAsync(MessageContext context, Exception exception);
    protected override ValueTask ProcessingStepAsync(CancellationToken cancellationToken);
}
