// Assembly 'System.Cloud.Messaging'

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

public interface IMessageSource
{
    ValueTask<MessageContext> ReadAsync(CancellationToken cancellationToken);
    void Release(MessageContext context);
}
