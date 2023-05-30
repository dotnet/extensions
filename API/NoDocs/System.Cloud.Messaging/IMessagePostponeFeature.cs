// Assembly 'System.Cloud.Messaging'

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

public interface IMessagePostponeFeature
{
    ValueTask PostponeAsync(TimeSpan delay, CancellationToken cancellationToken);
}
