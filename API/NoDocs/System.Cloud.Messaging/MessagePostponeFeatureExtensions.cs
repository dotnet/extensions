// Assembly 'System.Cloud.Messaging'

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

public static class MessagePostponeFeatureExtensions
{
    public static ValueTask PostponeAsync(this MessageContext context, TimeSpan delay, CancellationToken cancellationToken);
    public static void SetMessagePostponeFeature(this MessageContext context, IMessagePostponeFeature messagePostponeFeature);
}
