// Assembly 'System.Cloud.Messaging'

using System.Threading.Tasks;

namespace System.Cloud.Messaging;

public interface IMessageMiddleware
{
    ValueTask InvokeAsync(MessageContext context, MessageDelegate nextHandler);
}
