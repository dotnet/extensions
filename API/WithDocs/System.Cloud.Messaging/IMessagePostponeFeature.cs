// Assembly 'System.Cloud.Messaging'

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.Messaging;

/// <summary>
/// Feature interface for postponing the message processing.
/// </summary>
public interface IMessagePostponeFeature
{
    /// <summary>
    /// Postpones the message processing asynchronously.
    /// </summary>
    /// <param name="delay">The time span by which message processing is to be postponed.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns><see cref="T:System.Threading.Tasks.ValueTask" />.</returns>
    ValueTask PostponeAsync(TimeSpan delay, CancellationToken cancellationToken);
}
