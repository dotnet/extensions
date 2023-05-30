// Assembly 'System.Cloud.Messaging'

namespace System.Cloud.Messaging;

/// <summary>
/// Feature interface for setting/retrieving the visibility delay.
/// </summary>
public interface IMessageVisibilityDelayFeature
{
    /// <summary>
    /// Gets the visibility delay which represents the delay after which the message is available for other <see cref="T:System.Cloud.Messaging.MessageConsumer" /> to process.
    /// </summary>
    TimeSpan VisibilityDelay { get; }
}
